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

    // Tarif paramétrable (remplace progressivement les champs PrixPar* / Mode ci-dessus)
    public Guid? TarifId { get; set; }
    public Tarif? Tarif { get; set; }

    // Limites strictes par colis (au-delà → refus auto à la création de la commande)
    public int? LongueurMaxColisCm { get; set; }
    public int? LargeurMaxColisCm { get; set; }
    public int? HauteurMaxColisCm { get; set; }
    public decimal? PoidsMaxColisKg { get; set; }
    public string? PointDepot { get; set; }
    public Guid? RelaisDepartId { get; set; }
    public PointRelais? RelaisDepart { get; set; }
    public string? Conditions { get; set; }
    public StatutTrajet Statut { get; set; } = StatutTrajet.Actif;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Démarrage de la tournée (null = pas encore démarrée). Sert aux alertes "date dépassée non démarré".
    public DateTime? DateDemarrageTournee { get; set; }

    public Transporteur? Transporteur { get; set; }
    public ICollection<EtapeTrajet> Etapes { get; set; } = new List<EtapeTrajet>();
}
