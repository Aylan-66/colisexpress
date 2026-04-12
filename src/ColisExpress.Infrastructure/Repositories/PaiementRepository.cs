using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class PaiementRepository : IPaiementRepository
{
    private readonly ColisExpressDbContext _db;

    public PaiementRepository(ColisExpressDbContext db) => _db = db;

    public Task<Paiement?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Paiements.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Paiement>> GetByCommandeIdAsync(Guid commandeId, CancellationToken ct = default) =>
        await _db.Paiements.Where(p => p.CommandeId == commandeId).ToListAsync(ct);

    public async Task AddAsync(Paiement paiement, CancellationToken ct = default) =>
        await _db.Paiements.AddAsync(paiement, ct);

    public void Update(Paiement paiement) => _db.Paiements.Update(paiement);
}
