using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Auth;

/// <summary>
/// 회원 등록 요청 DTO
/// </summary>
public class RegisterRequest {
    /// <summary>
    /// 로그인 아이디
    /// </summary>
    [Required(ErrorMessage = "아이디는 필수입니다.")]
    [StringLength(50, ErrorMessage = "아이디는 50자 이하여야 합니다.")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 비밀번호
    /// </summary>
    [Required(ErrorMessage = "비밀번호는 필수입니다.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "비밀번호는 8자 이상 100자 이하여야 합니다.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 사용자명
    /// </summary>
    [Required(ErrorMessage = "사용자명은 필수입니다.")]
    [StringLength(50, ErrorMessage = "사용자명은 50자 이하여야 합니다.")]
    public string Name { get; set; } = string.Empty;
}