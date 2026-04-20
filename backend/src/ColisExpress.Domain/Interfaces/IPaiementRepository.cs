using ColisExpress.Domain.Entities;

namespace ColisExpress.Domain.Interfaces;

public interface IPaiementRepository
{
    Task<Paiement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Paiement>> GetByCommandeIdAsync(Guid commandeId, CancellationToken ct = default);
    Task AddAsync(Paiement paiement, CancellationToken ct = default);
    void Update(Paiement paiement);
}
