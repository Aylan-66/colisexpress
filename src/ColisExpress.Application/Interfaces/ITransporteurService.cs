using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.DTOs.Trajets;

namespace ColisExpress.Application.Interfaces;

public interface ITransporteurService
{
    Task<AuthResult> RegisterAsync(RegisterTransporteurRequest request, CancellationToken ct = default);
    Task<TransporteurDashboardResponse?> GetDashboardAsync(Guid utilisateurId, CancellationToken ct = default);
    Task<OperationResult> UploadDocumentAsync(UploadDocumentKycRequest request, CancellationToken ct = default);
    Task<(byte[]? Contenu, string? ContentType, string? NomFichier)> GetDocumentFileAsync(Guid documentId, CancellationToken ct = default);
}
