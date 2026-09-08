using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Auth;
using SmartWMS.Api.Models;
using SmartWMS.Api.Services;
using System.Security.Claims;

namespace SmartWMS.Api.Controllers;

/// <summary>
/// 인증 관련 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    private readonly SmartWmsDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        SmartWmsDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    // 회원 등록
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUserId =
            request.UserId.Trim().ToLowerInvariant();

        // 로그인 아이디 중복 확인
        var isDuplicateUserId = await _dbContext.Users
            .AnyAsync(
                x => x.UserId == normalizedUserId,
                cancellationToken);

        if (isDuplicateUserId) {
            return Conflict(new ApiErrorResponse {
                StatusCode = StatusCodes.Status409Conflict,
                Message = $"아이디 {normalizedUserId}는 이미 사용 중입니다."
            });
        }

        var user = new User {
            UserId = normalizedUserId,
            Name = request.Name.Trim(),
            Role = "USER"
        };

        // 비밀번호를 해시한 뒤 저장
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<object> {
            Success = true,
            Message = "회원 등록이 완료되었습니다.",
            Data = new {
                user.Id,
                user.UserId,
                user.Name,
                user.Role
            }
        });
    }

    // 로그인
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login(
    LoginRequest request,
    CancellationToken cancellationToken)
    {
        var normalizedUserId =
            request.UserId.Trim().ToLowerInvariant();

        // 사용자 조회
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.UserId == normalizedUserId,
                cancellationToken);

        if (user is null || !user.IsActive) { // 비활성화인 경우에도 구분하기 어렵도록
            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "아이디 또는 비밀번호가 올바르지 않습니다.",
                Detail = null
            });
        }

        // 입력한 비밀번호와 저장된 해시값 비교
        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed) {
            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "아이디 또는 비밀번호가 올바르지 않습니다."
            });
        }

        // 로그인 성공 시 JWT Access Token 생성
        var accessToken = _jwtTokenService.CreateToken(user);

        // Refresh Token 원문 생성
        var refreshToken =
            _jwtTokenService.CreateRefreshToken();

        // DB 저장용 Hash 생성
        var refreshTokenHash =
            _jwtTokenService.HashRefreshToken(
                refreshToken);

        // Refresh Token 만료 기간 조회
        var refreshTokenDays =
            _configuration.GetValue<int>(
                "Jwt:RefreshTokenDays");

        var now = DateTime.UtcNow;

        // Refresh Token Hash를 DB에 저장
        var refreshTokenEntity = new RefreshToken {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(
            refreshTokenEntity);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Ok(new ApiResponse<object> {
            Success = true,
            Message = "로그인에 성공했습니다.",
            Data = new {
                accessToken,
                refreshToken,
                user.Id,
                user.UserId,
                user.Name,
                user.Role
            }
        });
    }

    // 회원조회
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> GetMe(
    CancellationToken cancellationToken)
    {
        // JWT Claim에서 로그인한 사용자 Id 조회
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId)) {
            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "사용자 인증 정보를 확인할 수 없습니다."
            });
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "사용자 정보를 찾을 수 없습니다."
            });
        }

        return Ok(new ApiResponse<object> {
            Success = true,
            Message = "회원정보 조회에 성공했습니다.",
            Data = new {
                user.Id,
                user.UserId,
                user.Name,
                user.Role
            }
        });
    }

    // Access Token 재발급
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<object>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(request.RefreshToken)) {
            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Refresh Token이 유효하지 않습니다."
            });
        }

        var refreshTokenHash =
            _jwtTokenService.HashRefreshToken(
                request.RefreshToken);

        var savedRefreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == refreshTokenHash,
                cancellationToken);

        if (savedRefreshToken is null ||
            savedRefreshToken.RevokedAt is not null ||
            savedRefreshToken.ExpiresAt <= DateTime.UtcNow ||
            !savedRefreshToken.User.IsActive) {

            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Refresh Token이 유효하지 않습니다."
            });
        }

        var now = DateTime.UtcNow;

        // 기존 Refresh Token 폐기
        savedRefreshToken.RevokedAt = now;

        // 새로운 Access Token 생성
        var newAccessToken =
            _jwtTokenService.CreateToken(
                savedRefreshToken.User);

        // 새로운 Refresh Token 생성
        var newRefreshToken =
            _jwtTokenService.CreateRefreshToken();

        var newRefreshTokenHash =
            _jwtTokenService.HashRefreshToken(
                newRefreshToken);

        var refreshTokenDays =
            _configuration.GetValue<int>(
                "Jwt:RefreshTokenDays");

        var newRefreshTokenEntity = new RefreshToken {
            UserId = savedRefreshToken.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(
            newRefreshTokenEntity);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Ok(new ApiResponse<object> {
            Success = true,
            Message = "Token 재발급에 성공했습니다.",
            Data = new {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            }
        });
    }
}