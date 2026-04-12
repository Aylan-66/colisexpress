using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Utilisateur
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RoleUtilisateur Role { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public string? Adresse { get; set; }
    public StatutCompte StatutCompte { get; set; } = StatutCompte.EnAttente;
    public bool EmailVerifie { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DerniereConnexion { get; set; }
}
