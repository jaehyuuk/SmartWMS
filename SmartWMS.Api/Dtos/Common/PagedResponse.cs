namespace SmartWMS.Api.Dtos.Common;

public class PagedResponse<T> {
    /// <summary>
    /// 현재 페이지 데이터
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// 현재 페이지 번호
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 페이지당 데이터 수
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 전체 데이터 수
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 전체 페이지 수
    /// </summary>
    public int TotalPages { get; set; }
}