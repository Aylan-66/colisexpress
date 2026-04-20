using ColisExpress.Domain.Entities;

namespace ColisExpress.Domain.Interfaces;

public interface ICommandeRepository
{
    Task<Commande?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Commande>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<Commande>> GetByTransporteurIdAsync(Guid transporteurId, CancellationToken ct = default);
    Task AddAsync(Commande commande, CancellationToken ct = default);
    void Update(Commande commande);
}
