using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.DTOs.Colis;

namespace ColisExpress.Application.Interfaces;

public interface IAdminService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UtilisateurListItem>> GetUtilisateursAsync(string? search, CancellationToken ct = default);
    Task<IReadOnlyList<TransporteurListItem>> GetTransporteursAsync(CancellationToken ct = default);
    Task<OperationResult> DecideKycAsync(KycDecisionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PointRelaisListItem>> GetPointsRelaisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CommandeAdminListItem>> GetCommandesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PaiementAdminListItem>> GetPaiementsAsync(CancellationToken ct = default);
    Task<ColisDetailResponse?> GetColisByCodeAsync(string codeColis, CancellationToken ct = default);
}
