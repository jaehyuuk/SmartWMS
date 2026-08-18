using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartWMS.Api.Models;

namespace SmartWMS.Api.Services;

/// <summary>
/// JWT Access Token을 생성하는 서비스
/// </summary>
public class JwtTokenService {
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) {
        _configuration = configuration;
    }

    public string CreateToken(User user) {
        var jwtSection = _configuration.GetSection("Jwt");

        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer 설정이 없습니다.");

        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException("JWT Audience 설정이 없습니다.");

        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key 설정이 없습니다.");

        var expireMinutes =
            int.TryParse(jwtSection["ExpireMinutes"], out var minutes)
                ? minutes
                : 60;

        // 토큰에 포함할 사용자 정보
        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserId),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var securityKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}