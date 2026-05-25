using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ColisExpressDbContext _db;

    public UnitOfWork(
        ColisExpressDbContext db,
        IUtilisateurRepository utilisateurs,
        ITransporteurRepository transporteurs,
        ITrajetRepository trajets,
        ICommandeRepository commandes,
        IColisRepository colis,
        IPaiementRepository paiements)
    {
        _db = db;
        Utilisateurs = utilisateurs;
        Transporteurs = transporteurs;
        Trajets = trajets;
        Commandes = commandes;
        Colis = colis;
        Paiements = paiements;
    }

    public IUtilisateurRepository Utilisateurs { get; }
    public ITransporteurRepository Transporteurs { get; }
    public ITrajetRepository Trajets { get; }
    public ICommandeRepository Commandes { get; }
    public IColisRepository Colis { get; }
    public IPaiementRepository Paiements { get; }

    public async Task<ParametrePlateforme> GetParametresPlateformeAsync(CancellationToken ct = default)
    {
        var p = await _db.ParametresPlateforme.FirstOrDefaultAsync(ct);
        if (p is null)
        {
            p = new ParametrePlateforme();
            _db.ParametresPlateforme.Add(p);
            await _db.SaveChangesAsync(ct);
        }
        return p;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
