using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Auth;
using SmartWMS.Api.Models;
using SmartWMS.Api.Services;

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

    public AuthController(
        SmartWmsDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    // 회원 등록
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

        if (user is null) {
            return Unauthorized(new ApiErrorResponse {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "아이디 또는 비밀번호가 올바르지 않습니다."
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

        return Ok(new ApiResponse<object> {
            Success = true,
            Message = "로그인에 성공했습니다.",
            Data = new {
                accessToken,
                user.Id,
                user.UserId,
                user.Name,
                user.Role
            }
        });
    }
}