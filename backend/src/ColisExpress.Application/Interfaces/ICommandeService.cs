using ColisExpress.Application.DTOs.Commandes;

namespace ColisExpress.Application.Interfaces;

public enum FiltreCommandes
{
    Toutes,
    EnCours,
    Livrees,
    Annulees
}

public interface ICommandeService
{
    Task<CommandeResponse> CreateAsync(CreateCommandeRequest request, CancellationToken ct = default);
    Task<CommandeResponse?> GetByIdAsync(Guid commandeId, Guid clientId, CancellationToken ct = default);
    Task<CommandeDetailResponse?> GetDetailAsync(Guid commandeId, Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<CommandeListItem>> GetCommandesClientAsync(Guid clientId, FiltreCommandes filtre = FiltreCommandes.Toutes, CancellationToken ct = default);
    Task ConfirmerPaiementAsync(Guid commandeId, Guid clientId, string? referenceExterne = null, CancellationToken ct = default);
    Task<OperationResult> AnnulerAsync(Guid commandeId, Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<CommandeListItem>> GetCommandesTransporteurAsync(Guid utilisateurId, CancellationToken ct = default);
}
