using ColisExpress.Application.DTOs.Offres;

namespace ColisExpress.Application.Interfaces;

public interface IRechercheService
{
    Task<IReadOnlyList<OffreResponse>> RechercherAsync(RechercheOffreRequest request, CancellationToken ct = default);
    Task<OffreResponse?> GetOffreByTrajetIdAsync(Guid trajetId, decimal poids, CancellationToken ct = default);
}
