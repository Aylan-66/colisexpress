using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class TrajetRepository : ITrajetRepository
{
    private readonly ColisExpressDbContext _db;

    public TrajetRepository(ColisExpressDbContext db) => _db = db;

    public Task<Trajet?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Trajets.Include(t => t.Transporteur).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Trajet>> GetByTransporteurIdAsync(Guid transporteurId, CancellationToken ct = default) =>
        await _db.Trajets.Where(t => t.TransporteurId == transporteurId).ToListAsync(ct);

    public async Task<IReadOnlyList<Trajet>> SearchAsync(
        string villeDepart,
        string villeArrivee,
        DateTime dateMin,
        decimal poidsKg,
        CancellationToken ct = default)
    {
        return await _db.Trajets
            .Include(t => t.Transporteur)
                .ThenInclude(tr => tr!.Utilisateur)
            .Where(t => t.Statut == StatutTrajet.Actif
                && t.VilleDepart.ToLower() == villeDepart.ToLower()
                && t.VilleArrivee.ToLower() == villeArrivee.ToLower()
                && t.DateDepart >= dateMin
                && t.CapaciteMaxPoids >= poidsKg
                && t.CapaciteRestante > 0)
            .OrderBy(t => t.DateDepart)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetVillesDepartAsync(CancellationToken ct = default) =>
        await _db.Trajets
            .Where(t => t.Statut == StatutTrajet.Actif && t.CapaciteRestante > 0)
            .Select(t => t.VilleDepart)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetVillesArriveeAsync(CancellationToken ct = default) =>
        await _db.Trajets
            .Where(t => t.Statut == StatutTrajet.Actif && t.CapaciteRestante > 0)
            .Select(t => t.VilleArrivee)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

    public async Task AddAsync(Trajet trajet, CancellationToken ct = default) =>
        await _db.Trajets.AddAsync(trajet, ct);

    public void Update(Trajet trajet) => _db.Trajets.Update(trajet);

    public void Remove(Trajet trajet) => _db.Trajets.Remove(trajet);
}
