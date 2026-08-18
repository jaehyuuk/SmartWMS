using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace SmartWMS.Api.Common;

/// <summary>
/// 처리되지 않은 예외를 공통 형식으로 처리하는 전역 예외 처리기
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler {
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 처리되지 않은 예외가 발생했을 때 호출
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 실제 예외 내용은 서버 로그에 기록
        _logger.LogError(
            exception,
            "처리되지 않은 예외가 발생했습니다.");

        // 예외 종류에 따라 상태 코드와 메시지 결정
        var response = exception switch {
            DbUpdateException => new ApiErrorResponse {
                StatusCode = StatusCodes.Status409Conflict,
                Message = "데이터 저장 중 충돌이 발생했습니다.",
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : null
            },

            _ => new ApiErrorResponse {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "서버 내부 오류가 발생했습니다.",
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : null
            }
        };

        httpContext.Response.StatusCode = response.StatusCode;

        // 공통 오류 응답을 JSON으로 반환
        await httpContext.Response.WriteAsJsonAsync(
            response,
            cancellationToken);

        return true;
    }
}