using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Products;

public class UpdateProductRequest
{
    [Required(ErrorMessage = "상품 코드는 필수입니다.")]
    [StringLength(30, ErrorMessage = "상품 코드는 30자 이하여야 합니다.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "상품명은 필수입니다.")]
    [StringLength(100, ErrorMessage = "상품명은 100자 이하여야 합니다.")]
    public string Name { get; set; } = string.Empty;
}