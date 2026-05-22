namespace ColisExpress.Domain.Entities;

/// <summary>
/// Favori d'un client : un point relais ou un transporteur mis en favori pour le retrouver vite.
/// Exactement un des deux FK (PointRelaisId / TransporteurId) est renseigné.
/// </summary>
public class Favori
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Guid? PointRelaisId { get; set; }
    public Guid? TransporteurId { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Utilisateur? Client { get; set; }
    public PointRelais? PointRelais { get; set; }
    public Transporteur? Transporteur { get; set; }
}
