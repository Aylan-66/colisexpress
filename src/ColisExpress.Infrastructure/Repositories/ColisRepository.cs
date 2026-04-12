using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class ColisRepository : IColisRepository
{
    private readonly ColisExpressDbContext _db;

    public ColisRepository(ColisExpressDbContext db) => _db = db;

    public Task<Colis?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Colis.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Colis?> GetByCodeAsync(string codeColis, CancellationToken ct = default) =>
        _db.Colis.FirstOrDefaultAsync(c => c.CodeColis == codeColis, ct);

    public Task<Colis?> GetByCodeWithEvenementsAsync(string codeColis, CancellationToken ct = default) =>
        _db.Colis
            .Include(c => c.Commande)
                .ThenInclude(cmd => cmd!.Trajet)
            .Include(c => c.Evenements.OrderBy(e => e.DateHeure))
            .FirstOrDefaultAsync(c => c.CodeColis == codeColis, ct);

    public Task<bool> CodeColisExistsAsync(string codeColis, CancellationToken ct = default) =>
        _db.Colis.AnyAsync(c => c.CodeColis == codeColis, ct);

    public async Task AddAsync(Colis colis, CancellationToken ct = default) =>
        await _db.Colis.AddAsync(colis, ct);

    public async Task AddEvenementAsync(EvenementColis evenement, CancellationToken ct = default) =>
        await _db.EvenementsColis.AddAsync(evenement, ct);

    public void Update(Colis colis) => _db.Colis.Update(colis);
}
