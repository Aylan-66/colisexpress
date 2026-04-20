using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Auth;

public class AuthResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Guid UtilisateurId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Prenom { get; init; } = string.Empty;
    public string Nom { get; init; } = string.Empty;
    public RoleUtilisateur Role { get; init; }

    public static AuthResult Ok(Guid id, string email, string prenom, string nom, RoleUtilisateur role) =>
        new() { Success = true, UtilisateurId = id, Email = email, Prenom = prenom, Nom = nom, Role = role };

    public static AuthResult Fail(string error) => new() { Success = false, Error = error };
}
