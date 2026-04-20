using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Auth;

public class RegisterRequest
{
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public string ConfirmationMotDePasse { get; set; } = string.Empty;
    public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Client;
}
