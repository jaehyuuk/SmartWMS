namespace SmartWMS.Api.Models;

/// <summary>
/// 입고 DB
/// </summary>
public class Inbound {
    /// <summary>
    /// 입고 이력 고유번호
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 상품 코드
    /// </summary>
    public int ProductId { get; set; }
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
    /// <summary>
    /// Product Entity
    /// </summary>
    public Product Product { get; set; } = null!;
}