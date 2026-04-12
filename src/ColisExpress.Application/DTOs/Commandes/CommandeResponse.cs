using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Commandes;

public class CommandeResponse
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string CodeRetrait { get; init; } = string.Empty;
    public decimal PrixTransport { get; init; }
    public decimal FraisService { get; init; }
    public decimal SupplementsTotal { get; init; }
    public decimal Total { get; init; }
    public StatutColis StatutColis { get; init; }
    public StatutReglement StatutReglement { get; init; }
    public string VilleDepart { get; init; } = string.Empty;
    public string VilleArrivee { get; init; } = string.Empty;
    public DateTime DateDepart { get; init; }
    public string NomTransporteur { get; init; } = string.Empty;
    public DateTime DateCreation { get; init; }
}

public class CommandeListItem
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string Trajet { get; init; } = string.Empty;
    public string NomTransporteur { get; init; } = string.Empty;
    public StatutColis StatutColis { get; init; }
    public decimal Total { get; init; }
    public DateTime DateCreation { get; init; }
}

public class CommandeDetailResponse
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string CodeRetrait { get; init; } = string.Empty;
    public StatutColis StatutColis { get; init; }
    public StatutReglement StatutReglement { get; init; }
    public bool EstAnnulable { get; init; }

    public string VilleDepart { get; init; } = string.Empty;
    public string VilleArrivee { get; init; } = string.Empty;
    public DateTime DateDepart { get; init; }
    public DateTime DateEstimeeArrivee { get; init; }

    public string NomTransporteur { get; init; } = string.Empty;
    public string Initiales { get; init; } = string.Empty;
    public decimal NoteTransporteur { get; init; }
    public int NbAvisTransporteur { get; init; }
    public string? TypeVehicule { get; init; }

    public string NomDestinataire { get; init; } = string.Empty;
    public string TelephoneDestinataire { get; init; } = string.Empty;
    public string VilleDestinataire { get; init; } = string.Empty;

    public string DescriptionContenu { get; init; } = string.Empty;
    public decimal PoidsDeclare { get; init; }
    public string? Dimensions { get; init; }
    public decimal ValeurDeclaree { get; init; }
    public string? InstructionsParticulieres { get; init; }

    public decimal PrixTransport { get; init; }
    public decimal FraisService { get; init; }
    public decimal SupplementsTotal { get; init; }
    public decimal Total { get; init; }

    public DateTime DateCreation { get; init; }
}
