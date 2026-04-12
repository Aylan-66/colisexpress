using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class EvenementColis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ColisId { get; set; }
    public StatutColis AncienStatut { get; set; }
    public StatutColis NouveauStatut { get; set; }
    public DateTime DateHeure { get; set; } = DateTime.UtcNow;
    public Guid ActeurId { get; set; }
    public string? Commentaire { get; set; }
    public string? PhotoChemin { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public Colis? Colis { get; set; }
    public Utilisateur? Acteur { get; set; }
}
