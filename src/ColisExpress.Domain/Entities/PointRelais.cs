namespace ColisExpress.Domain.Entities;

public class PointRelais
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UtilisateurId { get; set; }
    public string NomRelais { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public bool EstActif { get; set; } = true;

    public Utilisateur? Utilisateur { get; set; }
}
