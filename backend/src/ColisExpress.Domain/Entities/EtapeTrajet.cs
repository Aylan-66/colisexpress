using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class EtapeTrajet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrajetId { get; set; }
    public Guid PointRelaisId { get; set; }
    public int Ordre { get; set; }
    public DateTime HeureEstimeeArrivee { get; set; }
    public DateTime? HeureReelleArrivee { get; set; }
    public bool RelaisOuvertALArrivee { get; set; }
    public StatutEtape Statut { get; set; } = StatutEtape.Planifiee;

    public Trajet? Trajet { get; set; }
    public PointRelais? PointRelais { get; set; }
}
