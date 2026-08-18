using System.ComponentModel.DataAnnotations;

namespace SmartWMS.Api.Dtos.Auth;

/// <summary>
/// 로그인 요청 DTO
/// </summary>
public class LoginRequest {
    /// <summary>
    /// 로그인 아이디
    /// </summary>
    [Required(ErrorMessage = "아이디는 필수입니다.")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 비밀번호
    /// </summary>
    [Required(ErrorMessage = "비밀번호는 필수입니다.")]
    public string Password { get; set; } = string.Empty;
}