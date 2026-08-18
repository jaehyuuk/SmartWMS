using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Outbounds;

/// <summary>
/// 출고 등록 요청 DTO
/// </summary>
public class CreateOutboundRequest {
    /// <summary>
    /// 상품 고유 번호
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "상품 Id는 1 이상이어야 합니다.")]
    public int ProductId { get; set; }

    /// <summary>
    /// 출고 수량
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "출고 수량은 1개 이상이어야 합니다.")]
    public int Quantity { get; set; }

    /// <summary>
    /// 비고
    /// </summary>
    [StringLength(200, ErrorMessage = "비고는 200자 이하여야 합니다.")]
    public string Memo { get; set; } = string.Empty;
}