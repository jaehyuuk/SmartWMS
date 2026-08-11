namespace SmartWMS.Api.Models;

public class Outbound {
    /// <summary>
    /// 출고 이력 고유 번호
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 상품 번호
    /// </summary>
    public int ProductId { get; set; }
    /// <summary>
    /// 출고 수량
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// 출고일시
    /// </summary>
    public DateTime OutboundDate { get; set; }
    /// <summary>
    /// 비고
    /// </summary>
    public string Memo { get; set; } = string.Empty;
    /// <summary>
    /// Product Entity
    /// </summary>
    public Product Product { get; set; } = null!;
}