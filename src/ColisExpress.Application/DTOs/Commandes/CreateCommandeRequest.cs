using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Commandes;

public class CreateCommandeRequest
{
    public Guid ClientId { get; set; }
    public Guid TrajetId { get; set; }

    public string NomDestinataire { get; set; } = string.Empty;
    public string TelephoneDestinataire { get; set; } = string.Empty;
    public string VilleDestinataire { get; set; } = string.Empty;

    public string DescriptionContenu { get; set; } = string.Empty;
    public decimal PoidsDeclare { get; set; }
    public string? Dimensions { get; set; }
    public decimal ValeurDeclaree { get; set; }

    public bool Fragile { get; set; }
    public bool Urgent { get; set; }
    public string? InstructionsParticulieres { get; set; }

    public ModeReglement ModeReglement { get; set; } = ModeReglement.Carte;
}
