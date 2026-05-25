using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Transporteur
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UtilisateurId { get; set; }
    public StatutKyc StatutKyc { get; set; } = StatutKyc.NonSoumis;
    public decimal NoteMoyenne { get; set; }
    public int NombreAvis { get; set; }
    public string? TypeVehicule { get; set; }
    public string? CorridorsActifs { get; set; }

    // Override des frais de service (null = utilise le défaut global de la plateforme)
    public TypeFraisService? FraisServiceType { get; set; }
    public decimal? FraisServiceValeur { get; set; }

    public Utilisateur? Utilisateur { get; set; }
    public ICollection<DocumentKyc> Documents { get; set; } = new List<DocumentKyc>();
}
