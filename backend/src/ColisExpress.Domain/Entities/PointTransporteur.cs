namespace ColisExpress.Domain.Entities;

/// <summary>
/// Point de dépôt / récupération personnel d'un transporteur (garage, local, atelier...).
/// Différent des PointRelais officiels — sert juste de référence rapide à réutiliser sur ses trajets.
/// </summary>
public class PointTransporteur
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransporteurId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Pays { get; set; } = "France";
    public string? Telephone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Transporteur? Transporteur { get; set; }
}
