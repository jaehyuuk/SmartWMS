using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Common;

public class PagedRequest {
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