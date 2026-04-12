using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class CommandeRepository : ICommandeRepository
{
    private readonly ColisExpressDbContext _db;

    public CommandeRepository(ColisExpressDbContext db) => _db = db;

    public Task<Commande?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Commandes
            .Include(c => c.Trajet)
            .Include(c => c.Transporteur)
            .Include(c => c.Colis)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Commande>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default) =>
        await _db.Commandes
            .Include(c => c.Trajet)
            .Include(c => c.Colis)
            .Where(c => c.ClientId == clientId)
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Commande>> GetByTransporteurIdAsync(Guid transporteurId, CancellationToken ct = default) =>
        await _db.Commandes
            .Where(c => c.TransporteurId == transporteurId)
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

    public async Task AddAsync(Commande commande, CancellationToken ct = default) =>
        await _db.Commandes.AddAsync(commande, ct);

    public void Update(Commande commande) => _db.Commandes.Update(commande);
}
