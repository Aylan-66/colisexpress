using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Colis;

public class ColisDetailResponse
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string CodeRetrait { get; init; } = string.Empty;
    public StatutColis Statut { get; init; }
    public decimal PoidsDeclare { get; init; }
    public decimal? PoidsReel { get; init; }
    public string? Dimensions { get; init; }
    public string VilleDepart { get; init; } = string.Empty;
    public string VilleArrivee { get; init; } = string.Empty;
    public string NomTransporteur { get; init; } = string.Empty;
    public string NomDestinataire { get; init; } = string.Empty;
    public DateTime DateCreation { get; init; }
    public IReadOnlyList<EvenementColisResponse> Evenements { get; init; } = Array.Empty<EvenementColisResponse>();
}

public class EvenementColisResponse
{
    public StatutColis NouveauStatut { get; init; }
    public DateTime DateHeure { get; init; }
    public string? Commentaire { get; init; }
}
