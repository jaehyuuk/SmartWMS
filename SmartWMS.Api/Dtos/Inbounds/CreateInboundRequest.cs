using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Inbounds {
    /// <summary>
    /// 입고 생성 제약
    /// </summary>
    public class CreateInboundRequest {
        [Range(1, int.MaxValue, ErrorMessage = "상품 Id는 1 이상이어야 합니다.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "입고 수량은 1개 이상이어야 합니다.")]
        public int Quantity { get; set; }

        [StringLength(200, ErrorMessage = "비고는 200자 이하여야 합니다.")]
        public string Memo { get; set; } = string.Empty;
    }
}
