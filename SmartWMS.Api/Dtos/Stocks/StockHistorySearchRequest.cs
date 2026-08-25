using SmartWMS.Api.Dtos.Common;

namespace SmartWMS.Api.Dtos.Stocks;

public class StockHistorySearchRequest : PagedRequest {
    /// <summary>
    /// 상품 코드 또는 상품명 검색어
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 재고 이력 타입
    /// INBOUND / OUTBOUND
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 조회 시작일
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 조회 종료일
    /// </summary>
    public DateTime? EndDate { get; set; }
}