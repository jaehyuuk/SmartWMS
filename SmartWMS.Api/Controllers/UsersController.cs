using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartWMS.Api.Common;
using SmartWMS.Api.Data;
using SmartWMS.Api.Dtos.Users;
using System.Security.Claims;

namespace SmartWMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ControllerBase
{
    private readonly SmartWmsDbContext _dbContext;

    public UsersController(SmartWmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserResponse>>>> GetUsers(
        CancellationToken cancellationToken)
    {

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new UserResponse {
                Id = x.Id,
                UserId = x.UserId,
                Name = x.Name,
                Role = x.Role,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<UserResponse>> {
            Success = true,
            Message = "사용자 목록을 조회했습니다.",
            Data = users
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(
    int id,
    CancellationToken cancellationToken)
    {

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserResponse {
                Id = x.Id,
                UserId = x.UserId,
                Name = x.Name,
                Role = x.Role,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "사용자를 찾을 수 없습니다.",
                Detail = $"Id가 {id}인 사용자가 존재하지 않습니다."
            });
        }

        return Ok(new ApiResponse<UserResponse> {
            Success = true,
            Message = "사용자 정보를 조회했습니다.",
            Data = user
        });
    }

    [HttpPut("{id:int}/role")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateRole(
    int id,
    UserRoleUpdateRequest request,
    CancellationToken cancellationToken)
    {

        var role = request.Role.Trim().ToUpperInvariant();

        if (role != "USER" &&
            role != "ADMIN") {
            return BadRequest(new ApiErrorResponse {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "사용자 권한이 올바르지 않습니다.",
                Detail = "Role은 USER 또는 ADMIN만 사용할 수 있습니다."
            });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "사용자를 찾을 수 없습니다.",
                Detail = $"Id가 {id}인 사용자가 존재하지 않습니다."
            });
        }

        var currentUserIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(currentUserIdClaim, out var currentUserId) &&
            currentUserId == id &&
            user.Role == "ADMIN" &&
            role != "ADMIN") {
            return BadRequest(new ApiErrorResponse {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "자신의 관리자 권한은 해제할 수 없습니다.",
                Detail = null
            });
        }

        user.Role = role;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var response = new UserResponse {
            Id = user.Id,
            UserId = user.UserId,
            Name = user.Name,
            Role = user.Role,
            IsActive = user.IsActive
        };

        return Ok(new ApiResponse<UserResponse> {
            Success = true,
            Message = "사용자 권한을 변경했습니다.",
            Data = response
        });
    }

    [HttpPut("{id:int}/active")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateActive(
    int id,
    UserActiveUpdateRequest request,
    CancellationToken cancellationToken)
    {

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null) {
            return NotFound(new ApiErrorResponse {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "사용자를 찾을 수 없습니다.",
                Detail = $"Id가 {id}인 사용자가 존재하지 않습니다."
            });
        }

        var currentUserIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(currentUserIdClaim, out var currentUserId) &&
            currentUserId == id &&
            !request.IsActive) {
            return BadRequest(new ApiErrorResponse {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "자신의 계정은 비활성화할 수 없습니다.",
                Detail = null
            });
        }

        user.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var response = new UserResponse {
            Id = user.Id,
            UserId = user.UserId,
            Name = user.Name,
            Role = user.Role,
            IsActive = user.IsActive
        };

        return Ok(new ApiResponse<UserResponse> {
            Success = true,
            Message = request.IsActive
                ? "사용자 계정을 활성화했습니다."
                : "사용자 계정을 비활성화했습니다.",
            Data = response
        });
    }
}