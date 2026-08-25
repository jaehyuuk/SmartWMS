using SmartWMS.Api.Dtos.Common;

namespace SmartWMS.Api.Dtos.Products;

public class ProductSearchRequest : PagedRequest {
    /// <summary>
    /// 상품 코드 또는 상품명 검색어
    /// </summary>
    public string? Keyword { get; set; }
}