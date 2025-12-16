using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using LlamaApi.Core.Configuration;

namespace LlamaApi.Services.Auth;

public class JwtAuthService(IOptions<JwtSettings> jwtSettings)
{
    private readonly string _secretKey = jwtSettings.Value.SecretKey;
    private string? _devToken;

  public void SeedDevToken()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "dev"), new Claim("scope", "api") };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "llama-api",
            audience: "llama-api",
            claims: claims,
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: creds
        );
        _devToken = new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? GetDevToken() => _devToken;

    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        if (token == _devToken) return true;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = "llama-api",
                ValidateAudience = true,
                ValidAudience = "llama-api",
                ClockSkew = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
