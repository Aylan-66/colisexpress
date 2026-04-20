using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Commande
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Guid TransporteurId { get; set; }
    public Guid TrajetId { get; set; }
    public Guid? RelaisDepartId { get; set; }
    public Guid? RelaisArriveeId { get; set; }

    public string NomDestinataire { get; set; } = string.Empty;
    public string TelephoneDestinataire { get; set; } = string.Empty;
    public string VilleDestinataire { get; set; } = string.Empty;

    public string DescriptionContenu { get; set; } = string.Empty;
    public decimal PoidsDeclare { get; set; }
    public string? Dimensions { get; set; }
    public decimal ValeurDeclaree { get; set; }

    public decimal PrixTransport { get; set; }
    public decimal FraisService { get; set; }
    public decimal SupplementsTotal { get; set; }
    public decimal Total { get; set; }

    public ModeReglement ModeReglement { get; set; }
    public StatutReglement StatutReglement { get; set; } = StatutReglement.EnAttente;

    public string? InstructionsParticulieres { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Utilisateur? Client { get; set; }
    public Transporteur? Transporteur { get; set; }
    public Trajet? Trajet { get; set; }
    public PointRelais? RelaisDepart { get; set; }
    public PointRelais? RelaisArrivee { get; set; }
    public Colis? Colis { get; set; }
}
