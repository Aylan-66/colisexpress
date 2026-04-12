using ColisExpress.Domain.Entities;

namespace ColisExpress.Domain.Interfaces;

public interface IColisRepository
{
    Task<Colis?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Colis?> GetByCodeAsync(string codeColis, CancellationToken ct = default);
    Task<Colis?> GetByCodeWithEvenementsAsync(string codeColis, CancellationToken ct = default);
    Task<bool> CodeColisExistsAsync(string codeColis, CancellationToken ct = default);
    Task AddAsync(Colis colis, CancellationToken ct = default);
    Task AddEvenementAsync(EvenementColis evenement, CancellationToken ct = default);
    void Update(Colis colis);
}
