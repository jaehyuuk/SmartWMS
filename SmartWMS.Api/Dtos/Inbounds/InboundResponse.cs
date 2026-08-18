namespace SmartWMS.Api.Dtos.Inbounds;

/// <summary>
/// 입고 이력을 반환하기 위한 응답 DTO
/// </summary>
public class InboundResponse {
    /// <summary>
    /// 입고 이력 고유 번호
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 상품 고유 번호
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 상품 코드
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// 상품명
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 입고 수량
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 입고 일시
    /// </summary>
    public DateTime InboundDate { get; set; }

    /// <summary>
    /// 비고
    /// </summary>
    public string Memo { get; set; } = string.Empty;
}