namespace SmartWMS.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }

    // Refresh Token 소유 사용자
    public int UserId { get; set; }

    // Refresh Token 원문 대신 Hash 저장
    public string TokenHash { get; set; } = string.Empty;

    // Token 만료 시간
    public DateTime ExpiresAt { get; set; }

    // Token 발급 시간
    public DateTime CreatedAt { get; set; }

    // 폐기 시간, null이면 사용 가능
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}