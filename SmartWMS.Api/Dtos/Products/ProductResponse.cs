namespace SmartWMS.Api.Dtos.Products;

/// <summary>
/// 상품 정보를 클라이언트에 반환하기 위한 응답 DTO
/// </summary>
public class ProductResponse {
    /// <summary>
    /// 상품 고유 Id
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