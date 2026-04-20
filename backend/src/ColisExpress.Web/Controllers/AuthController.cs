using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ITransporteurService _transporteur;
    private readonly IJwtService _jwt;

    public AuthController(IAuthService auth, ITransporteurService transporteur, IJwtService jwt)
    {
        _auth = auth;
        _transporteur = transporteur;
        _jwt = jwt;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });

        var token = _jwt.GenerateToken(result.UtilisateurId, result.Email, result.Prenom, result.Nom, result.Role.ToString());
        return Ok(new
        {
            utilisateurId = result.UtilisateurId,
            email = result.Email,
            prenom = result.Prenom,
            nom = result.Nom,
            role = result.Role.ToString(),
            accessToken = token.AccessToken,
            refreshToken = token.RefreshToken,
            expiresAt = token.ExpiresAt
        });
    }

    [HttpPost("register/transporteur")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterTransporteur([FromBody] RegisterTransporteurRequest request, CancellationToken ct)
    {
        var result = await _transporteur.RegisterAsync(request, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });

        var token = _jwt.GenerateToken(result.UtilisateurId, result.Email, result.Prenom, result.Nom, result.Role.ToString());
        return Ok(new
        {
            utilisateurId = result.UtilisateurId,
            email = result.Email,
            prenom = result.Prenom,
            nom = result.Nom,
            role = result.Role.ToString(),
            accessToken = token.AccessToken,
            refreshToken = token.RefreshToken,
            expiresAt = token.ExpiresAt
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        if (!result.Success) return Unauthorized(new { error = result.Error });

        var token = _jwt.GenerateToken(result.UtilisateurId, result.Email, result.Prenom, result.Nom, result.Role.ToString());
        return Ok(new
        {
            utilisateurId = result.UtilisateurId,
            email = result.Email,
            prenom = result.Prenom,
            nom = result.Nom,
            role = result.Role.ToString(),
            accessToken = token.AccessToken,
            refreshToken = token.RefreshToken,
            expiresAt = token.ExpiresAt
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var validation = _jwt.ValidateRefreshToken(request.RefreshToken);
        if (validation is null) return Unauthorized(new { error = "Refresh token invalide ou expiré." });

        var (_, userId) = validation.Value;
        var result = await _auth.LoginByIdAsync(userId, ct);
        if (result is null) return Unauthorized(new { error = "Utilisateur introuvable." });

        _jwt.RevokeRefreshToken(request.RefreshToken);
        var token = _jwt.GenerateToken(result.UtilisateurId, result.Email, result.Prenom, result.Nom, result.Role.ToString());
        return Ok(new
        {
            accessToken = token.AccessToken,
            refreshToken = token.RefreshToken,
            expiresAt = token.ExpiresAt
        });
    }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
