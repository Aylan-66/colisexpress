using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.Interfaces;

public interface IAdminService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UtilisateurListItem>> GetUtilisateursAsync(string? search, CancellationToken ct = default);
    Task<OperationResult> SuspendreCompteAsync(Guid utilisateurId, CancellationToken ct = default);
    Task<OperationResult> ReactiverCompteAsync(Guid utilisateurId, CancellationToken ct = default);
    Task<IReadOnlyList<TransporteurListItem>> GetTransporteursAsync(CancellationToken ct = default);
    Task<OperationResult> DecideKycAsync(KycDecisionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PointRelaisListItem>> GetPointsRelaisAsync(CancellationToken ct = default);
    Task<OperationResult> CreatePointRelaisAsync(CreatePointRelaisRequest request, CancellationToken ct = default);
    Task<OperationResult> TogglePointRelaisAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CommandeAdminListItem>> GetCommandesAsync(string? statutFilter, CancellationToken ct = default);
    Task<CommandeAdminDetail?> GetCommandeDetailAsync(Guid commandeId, CancellationToken ct = default);
    Task<IReadOnlyList<PaiementAdminListItem>> GetPaiementsAsync(string? modeFilter, CancellationToken ct = default);
    Task<ColisDetailResponse?> GetColisByCodeAsync(string codeColis, CancellationToken ct = default);
    Task<OperationResult> ResoudreLitigeAsync(Guid commandeId, string commentaire, CancellationToken ct = default);
    Task<IReadOnlyList<UtilisateurListItem>> RechercheGlobaleAsync(string query, CancellationToken ct = default);
}
