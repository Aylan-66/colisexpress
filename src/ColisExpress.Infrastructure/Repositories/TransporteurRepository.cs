using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class TransporteurRepository : ITransporteurRepository
{
    private readonly ColisExpressDbContext _db;

    public TransporteurRepository(ColisExpressDbContext db) => _db = db;

    public Task<Transporteur?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Transporteurs.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Transporteur?> GetByUtilisateurIdAsync(Guid utilisateurId, CancellationToken ct = default) =>
        _db.Transporteurs.FirstOrDefaultAsync(t => t.UtilisateurId == utilisateurId, ct);

    public async Task<IReadOnlyList<Transporteur>> GetByStatutKycAsync(StatutKyc statut, CancellationToken ct = default) =>
        await _db.Transporteurs.Where(t => t.StatutKyc == statut).ToListAsync(ct);

    public async Task AddAsync(Transporteur transporteur, CancellationToken ct = default) =>
        await _db.Transporteurs.AddAsync(transporteur, ct);

    public void Update(Transporteur transporteur) => _db.Transporteurs.Update(transporteur);
}
