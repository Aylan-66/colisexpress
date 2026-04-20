using ColisExpress.Application.DTOs.Colis;

namespace ColisExpress.Application.Interfaces;

public interface IColisService
{
    Task<ColisDetailResponse?> GetByCodeAsync(string codeColis, CancellationToken ct = default);
}
