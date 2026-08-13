namespace SmartWMS.Api.Common;

/// <summary>
/// API 성공 응답을 공통된 형태로 반환하기 위한 응답 객체
/// </summary>
public class ApiResponse<T> {
    /// <summary>
    /// 요청 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 클라이언트에게 전달할 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 실제 응답 데이터
    /// </summary>
    public T? Data { get; set; }
}