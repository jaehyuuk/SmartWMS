namespace SmartWMS.Api.Models;

/// <summary>
/// 상품 정보를 저장하는 Entity
/// </summary>
public class Product {
    /// <summary>
    /// 상품 고유 번호
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 상품 코드
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 상품명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 현재 재고 수량
    /// </summary>
    public int StockQuantity { get; set; }
}