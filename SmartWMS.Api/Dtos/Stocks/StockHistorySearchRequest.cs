using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Stocks;

public class StockHistorySearchRequest {
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

    /// <summary>
    /// 현재 페이지
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 페이지당 데이터 수
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}