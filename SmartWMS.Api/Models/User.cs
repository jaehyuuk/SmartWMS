namespace SmartWMS.Api.Models;

/// <summary>
/// 사용자 정보를 저장하는 Entity
/// </summary>
public class User {
    /// <summary>
    /// 사용자 고유 번호
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 로그인 아이디
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 비밀번호 해시값
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 사용자명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 사용자 권한
    /// </summary>
    public string Role { get; set; } = "USER";
}