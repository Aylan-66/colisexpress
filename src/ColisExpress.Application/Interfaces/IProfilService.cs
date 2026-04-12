using ColisExpress.Application.DTOs.Profil;

namespace ColisExpress.Application.Interfaces;

public class OperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public static OperationResult Ok() => new() { Success = true };
    public static OperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IProfilService
{
    Task<UpdateProfilRequest?> GetProfilAsync(Guid utilisateurId, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(Guid utilisateurId, UpdateProfilRequest request, CancellationToken ct = default);
    Task<OperationResult> ChangerMotDePasseAsync(Guid utilisateurId, ChangerMotDePasseRequest request, CancellationToken ct = default);
    Task<ProfilStatsResponse> GetStatsAsync(Guid utilisateurId, CancellationToken ct = default);
}
