namespace SmartWMS.Api.Common;

/// <summary>
/// API 오류를 공통된 형태로 반환하기 위한 응답 객체
/// </summary>
public class ApiErrorResponse {
    /// <summary>
    /// HTTP 상태 코드
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 오류 상세 정보
    /// </summary>
    public string? Detail { get; set; }
}