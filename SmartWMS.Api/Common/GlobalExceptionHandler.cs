using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartWMS.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace SmartWMS.Api.Common;

/// <summary>
/// 애플리케이션에서 처리되지 않은 예외를 공통 형식으로 응답하기 위한 전역 예외 처리기
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler {
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 처리되지 않은 예외가 발생했을 때 호출됨
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 서버 로그에는 실제 예외 내용을 기록
        _logger.LogError(
            exception,
            "처리되지 않은 예외가 발생했습니다.");

        // 예외 종류에 따라 HTTP 상태 코드와 메시지 결정
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

        // HTTP Status Code 설정
        httpContext.Response.StatusCode = response.StatusCode;

        // JSON 형태로 오류 응답 반환
        await httpContext.Response.WriteAsJsonAsync(
            response,
            cancellationToken);

        // true를 반환하면 해당 예외를 처리했다는 의미
        return true;
    }
}