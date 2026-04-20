using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ColisExpress.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ColisExpress.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTime Expires)> _refreshTokens = new();

    public JwtService(IOptions<JwtOptions> options) => _options = options.Value;

    public JwtTokenResult GenerateToken(Guid utilisateurId, string email, string prenom, string nom, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, utilisateurId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, $"{prenom} {nom}"),
            new Claim(ClaimTypes.Role, role),
            new Claim("Prenom", prenom),
            new Claim("Nom", nom)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();
        _refreshTokens[refreshToken] = (utilisateurId, DateTime.UtcNow.AddDays(_options.RefreshExpirationDays));

        return new JwtTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public (bool Valid, Guid UserId)? ValidateRefreshToken(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var data))
            return null;

        if (data.Expires < DateTime.UtcNow)
        {
            _refreshTokens.TryRemove(refreshToken, out _);
            return null;
        }

        return (true, data.UserId);
    }

    public void RevokeRefreshToken(string refreshToken) =>
        _refreshTokens.TryRemove(refreshToken, out _);

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
