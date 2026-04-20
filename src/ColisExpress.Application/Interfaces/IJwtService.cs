namespace ColisExpress.Application.Interfaces;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ColisExpress";
    public string Audience { get; set; } = "ColisExpressApp";
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshExpirationDays { get; set; } = 30;
}

public class JwtTokenResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public interface IJwtService
{
    JwtTokenResult GenerateToken(Guid utilisateurId, string email, string prenom, string nom, string role);
    (bool Valid, Guid UserId)? ValidateRefreshToken(string refreshToken);
    void RevokeRefreshToken(string refreshToken);
}
