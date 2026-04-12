using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Trajet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransporteurId { get; set; }
    public string PaysDepart { get; set; } = string.Empty;
    public string VilleDepart { get; set; } = string.Empty;
    public string PaysArrivee { get; set; } = string.Empty;
    public string VilleArrivee { get; set; } = string.Empty;
    public DateTime DateDepart { get; set; }
    public DateTime DateEstimeeArrivee { get; set; }
    public decimal CapaciteMaxPoids { get; set; }
    public int NombreMaxColis { get; set; }
    public int CapaciteRestante { get; set; }
    public ModeTarification ModeTarification { get; set; }
    public decimal? PrixParColis { get; set; }
    public decimal? PrixAuKilo { get; set; }
    public decimal? SupplementUrgent { get; set; }
    public decimal? SupplementFragile { get; set; }
    public string? PointDepot { get; set; }
    public string? Conditions { get; set; }
    public StatutTrajet Statut { get; set; } = StatutTrajet.Actif;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Transporteur? Transporteur { get; set; }
}
