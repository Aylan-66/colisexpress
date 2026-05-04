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
        var dep = villeDepart.ToLower();
        var arr = villeArrivee.ToLower();

        return await _db.Trajets
            .Include(t => t.Transporteur)
                .ThenInclude(tr => tr!.Utilisateur)
            .Include(t => t.RelaisDepart)
            .Include(t => t.Etapes)
                .ThenInclude(e => e.PointRelais)
            .Where(t => t.Statut == StatutTrajet.Actif
                && t.CapaciteMaxPoids >= poidsKg
                && t.CapaciteRestante > 0
                && (
                    // Match direct : VilleDepart → VilleArrivee avec date trajet
                    (t.VilleDepart.ToLower() == dep && t.VilleArrivee.ToLower() == arr && t.DateDepart >= dateMin)
                    ||
                    // Match via étapes : vérifie la date de l'étape de départ (pas du trajet)
                    (
                        (t.VilleDepart.ToLower() == dep || t.Etapes.Any(e => e.PointRelais!.Ville.ToLower() == dep))
                        && (t.VilleArrivee.ToLower() == arr || t.Etapes.Any(e => e.PointRelais!.Ville.ToLower() == arr))
                        && (
                            t.DateDepart >= dateMin
                            || t.Etapes.Any(e => e.PointRelais!.Ville.ToLower() == dep && e.HeureEstimeeArrivee >= dateMin)
                        )
                    )
                ))
            .OrderBy(t => t.DateDepart)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetVillesDepartAsync(CancellationToken ct = default)
    {
        var villesTrajet = await _db.Trajets
            .Where(t => t.Statut == StatutTrajet.Actif && t.CapaciteRestante > 0)
            .Select(t => t.VilleDepart)
            .ToListAsync(ct);

        var villesEtapes = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .Include(e => e.Trajet)
            .Where(e => e.Trajet!.Statut == StatutTrajet.Actif && e.Trajet.CapaciteRestante > 0)
            .Select(e => e.PointRelais!.Ville)
            .ToListAsync(ct);

        return villesTrajet.Union(villesEtapes).Distinct().OrderBy(v => v).ToList();
    }

    public async Task<IReadOnlyList<string>> GetVillesArriveeAsync(CancellationToken ct = default)
    {
        var villesTrajet = await _db.Trajets
            .Where(t => t.Statut == StatutTrajet.Actif && t.CapaciteRestante > 0)
            .Select(t => t.VilleArrivee)
            .ToListAsync(ct);

        var villesEtapes = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .Include(e => e.Trajet)
            .Where(e => e.Trajet!.Statut == StatutTrajet.Actif && e.Trajet.CapaciteRestante > 0)
            .Select(e => e.PointRelais!.Ville)
            .ToListAsync(ct);

        return villesTrajet.Union(villesEtapes).Distinct().OrderBy(v => v).ToList();
    }

    public async Task AddAsync(Trajet trajet, CancellationToken ct = default) =>
        await _db.Trajets.AddAsync(trajet, ct);

    public void Update(Trajet trajet) => _db.Trajets.Update(trajet);

    public void Remove(Trajet trajet) => _db.Trajets.Remove(trajet);
}
