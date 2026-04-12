using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ColisExpressDbContext _db;
    private readonly IColisService _colisService;

    public AdminService(ColisExpressDbContext db, IColisService colisService)
    {
        _db = db;
        _colisService = colisService;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var debutMois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var colisCeMois = await _db.Colis.CountAsync(c => c.DateCreation >= debutMois, ct);
        var colisLivres = await _db.Colis.CountAsync(c => c.Statut == StatutColis.LivraisonCloturee, ct);
        var transporteursActifs = await _db.Transporteurs.CountAsync(t => t.StatutKyc == StatutKyc.Valide, ct);
        var incidents = await _db.Colis.CountAsync(c =>
            c.Statut == StatutColis.Incident ||
            c.Statut == StatutColis.Endommage ||
            c.Statut == StatutColis.Perdu ||
            c.Statut == StatutColis.Refuse, ct);
        var kycEnAttente = await _db.Transporteurs.CountAsync(t => t.StatutKyc == StatutKyc.EnAttente, ct);

        var commandesRecentes = await _db.Commandes
            .Include(c => c.Trajet)
            .Include(c => c.Colis)
            .Include(c => c.Client)
            .OrderByDescending(c => c.DateCreation)
            .Take(10)
            .Select(c => new CommandeRecenteItem
            {
                Id = c.Id,
                CodeColis = c.Colis == null ? "—" : c.Colis.CodeColis,
                Trajet = (c.Trajet == null ? "—" : c.Trajet.VilleDepart + " → " + c.Trajet.VilleArrivee),
                Client = c.Client == null ? "—" : c.Client.Prenom + " " + c.Client.Nom,
                Statut = c.Colis == null ? StatutColis.Brouillon : c.Colis.Statut,
                Total = c.Total,
                DateCreation = c.DateCreation
            })
            .ToListAsync(ct);

        var transporteursAValider = await _db.Transporteurs
            .Include(t => t.Utilisateur)
            .Where(t => t.StatutKyc == StatutKyc.EnAttente)
            .Take(10)
            .Select(t => new TransporteurListItem
            {
                Id = t.Id,
                Nom = t.Utilisateur == null ? "—" : t.Utilisateur.Prenom + " " + t.Utilisateur.Nom,
                Email = t.Utilisateur == null ? "—" : t.Utilisateur.Email,
                Telephone = t.Utilisateur == null ? "—" : t.Utilisateur.Telephone,
                StatutKyc = t.StatutKyc,
                NoteMoyenne = t.NoteMoyenne,
                NombreAvis = t.NombreAvis,
                TypeVehicule = t.TypeVehicule
            })
            .ToListAsync(ct);

        return new DashboardResponse
        {
            ColisCeMois = colisCeMois,
            ColisLivres = colisLivres,
            TransporteursActifs = transporteursActifs,
            Incidents = incidents,
            TransporteursEnAttenteKyc = kycEnAttente,
            CommandesRecentes = commandesRecentes,
            TransporteursAValider = transporteursAValider
        };
    }

    public async Task<IReadOnlyList<UtilisateurListItem>> GetUtilisateursAsync(string? search, CancellationToken ct = default)
    {
        var query = _db.Utilisateurs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.Nom.ToLower().Contains(s) ||
                u.Prenom.ToLower().Contains(s));
        }
        return await query
            .OrderByDescending(u => u.DateCreation)
            .Take(100)
            .Select(u => new UtilisateurListItem
            {
                Id = u.Id,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Email = u.Email,
                Role = u.Role,
                StatutCompte = u.StatutCompte,
                DateCreation = u.DateCreation
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TransporteurListItem>> GetTransporteursAsync(CancellationToken ct = default)
    {
        return await _db.Transporteurs
            .Include(t => t.Utilisateur)
            .OrderBy(t => t.StatutKyc)
            .Select(t => new TransporteurListItem
            {
                Id = t.Id,
                Nom = t.Utilisateur == null ? "—" : t.Utilisateur.Prenom + " " + t.Utilisateur.Nom,
                Email = t.Utilisateur == null ? "—" : t.Utilisateur.Email,
                Telephone = t.Utilisateur == null ? "—" : t.Utilisateur.Telephone,
                StatutKyc = t.StatutKyc,
                NoteMoyenne = t.NoteMoyenne,
                NombreAvis = t.NombreAvis,
                TypeVehicule = t.TypeVehicule
            })
            .ToListAsync(ct);
    }

    public async Task<OperationResult> DecideKycAsync(KycDecisionRequest request, CancellationToken ct = default)
    {
        var transporteur = await _db.Transporteurs.FirstOrDefaultAsync(t => t.Id == request.TransporteurId, ct);
        if (transporteur is null) return OperationResult.Fail("Transporteur introuvable.");

        transporteur.StatutKyc = request.Approuver ? StatutKyc.Valide : StatutKyc.Rejete;
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<PointRelaisListItem>> GetPointsRelaisAsync(CancellationToken ct = default)
    {
        return await _db.PointsRelais
            .OrderBy(p => p.Pays).ThenBy(p => p.Ville)
            .Select(p => new PointRelaisListItem
            {
                Id = p.Id,
                NomRelais = p.NomRelais,
                Ville = p.Ville,
                Pays = p.Pays,
                Telephone = p.Telephone,
                EstActif = p.EstActif
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommandeAdminListItem>> GetCommandesAsync(CancellationToken ct = default)
    {
        return await _db.Commandes
            .Include(c => c.Trajet)
            .Include(c => c.Colis)
            .Include(c => c.Client)
            .Include(c => c.Transporteur).ThenInclude(t => t!.Utilisateur)
            .OrderByDescending(c => c.DateCreation)
            .Take(200)
            .Select(c => new CommandeAdminListItem
            {
                Id = c.Id,
                CodeColis = c.Colis == null ? "—" : c.Colis.CodeColis,
                Trajet = (c.Trajet == null ? "—" : c.Trajet.VilleDepart + " → " + c.Trajet.VilleArrivee),
                Client = c.Client == null ? "—" : c.Client.Prenom + " " + c.Client.Nom,
                Transporteur = (c.Transporteur == null || c.Transporteur.Utilisateur == null)
                    ? "—"
                    : c.Transporteur.Utilisateur.Prenom + " " + c.Transporteur.Utilisateur.Nom,
                StatutColis = c.Colis == null ? StatutColis.Brouillon : c.Colis.Statut,
                StatutReglement = c.StatutReglement,
                Total = c.Total,
                DateCreation = c.DateCreation
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PaiementAdminListItem>> GetPaiementsAsync(CancellationToken ct = default)
    {
        return await _db.Paiements
            .Include(p => p.Commande).ThenInclude(c => c!.Client)
            .Include(p => p.Commande).ThenInclude(c => c!.Colis)
            .OrderByDescending(p => p.DateCreation)
            .Take(200)
            .Select(p => new PaiementAdminListItem
            {
                Id = p.Id,
                CodeColis = p.Commande == null || p.Commande.Colis == null ? "—" : p.Commande.Colis.CodeColis,
                Client = p.Commande == null || p.Commande.Client == null
                    ? "—"
                    : p.Commande.Client.Prenom + " " + p.Commande.Client.Nom,
                Mode = p.Mode,
                Montant = p.Montant,
                Statut = p.Statut,
                DateCreation = p.DateCreation
            })
            .ToListAsync(ct);
    }

    public Task<ColisDetailResponse?> GetColisByCodeAsync(string codeColis, CancellationToken ct = default) =>
        _colisService.GetByCodeAsync(codeColis, ct);
}
