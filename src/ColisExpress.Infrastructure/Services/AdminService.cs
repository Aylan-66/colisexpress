using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8602

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

    public async Task<OperationResult> SuspendreCompteAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var u = await _db.Utilisateurs.FirstOrDefaultAsync(x => x.Id == utilisateurId, ct);
        if (u is null) return OperationResult.Fail("Utilisateur introuvable.");
        u.StatutCompte = StatutCompte.Suspendu;
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ReactiverCompteAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var u = await _db.Utilisateurs.FirstOrDefaultAsync(x => x.Id == utilisateurId, ct);
        if (u is null) return OperationResult.Fail("Utilisateur introuvable.");
        u.StatutCompte = StatutCompte.Actif;
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

    public async Task<OperationResult> CreatePointRelaisAsync(CreatePointRelaisRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NomRelais)) return OperationResult.Fail("Le nom du relais est obligatoire.");
        if (string.IsNullOrWhiteSpace(request.Ville)) return OperationResult.Fail("La ville est obligatoire.");
        if (string.IsNullOrWhiteSpace(request.Pays)) return OperationResult.Fail("Le pays est obligatoire.");

        var email = $"relais.{Guid.NewGuid().ToString()[..8]}@colisexpress.fr";
        var user = new Domain.Entities.Utilisateur
        {
            Role = RoleUtilisateur.PointRelais,
            Prenom = "Relais",
            Nom = request.NomRelais,
            Email = email,
            Telephone = request.Telephone,
            MotDePasseHash = "nologin",
            StatutCompte = StatutCompte.Actif,
            EmailVerifie = false
        };
        await _db.Utilisateurs.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        var relais = new Domain.Entities.PointRelais
        {
            UtilisateurId = user.Id,
            NomRelais = request.NomRelais.Trim(),
            Adresse = request.Adresse.Trim(),
            Ville = request.Ville.Trim(),
            Pays = request.Pays.Trim(),
            Telephone = request.Telephone.Trim(),
            EstActif = true
        };
        await _db.PointsRelais.AddAsync(relais, ct);
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> TogglePointRelaisAsync(Guid id, CancellationToken ct = default)
    {
        var relais = await _db.PointsRelais.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (relais is null) return OperationResult.Fail("Point relais introuvable.");
        relais.EstActif = !relais.EstActif;
        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<CommandeAdminListItem>> GetCommandesAsync(string? statutFilter, CancellationToken ct = default)
    {
        var query = _db.Commandes
            .Include(c => c.Trajet)
            .Include(c => c.Colis)
            .Include(c => c.Client)
            .Include(c => c.Transporteur).ThenInclude(t => t!.Utilisateur)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statutFilter) && Enum.TryParse<StatutColis>(statutFilter, out var statut))
            query = query.Where(c => c.Colis != null && c.Colis.Statut == statut);

        return await query
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

    public async Task<CommandeAdminDetail?> GetCommandeDetailAsync(Guid commandeId, CancellationToken ct = default)
    {
        var c = await _db.Commandes
            .Include(x => x.Trajet)
            .Include(x => x.Colis)
            .Include(x => x.Client)
            .Include(x => x.Transporteur).ThenInclude(t => t!.Utilisateur)
            .FirstOrDefaultAsync(x => x.Id == commandeId, ct);
        if (c is null) return null;

        var nomT = (c.Transporteur?.Utilisateur is not null)
            ? $"{c.Transporteur.Utilisateur.Prenom} {c.Transporteur.Utilisateur.Nom}" : "—";

        return new CommandeAdminDetail
        {
            Id = c.Id,
            CodeColis = c.Colis?.CodeColis ?? "—",
            CodeRetrait = c.Colis?.CodeRetrait ?? "—",
            StatutColis = c.Colis?.Statut ?? StatutColis.Brouillon,
            StatutReglement = c.StatutReglement,
            Client = c.Client is null ? "—" : $"{c.Client.Prenom} {c.Client.Nom}",
            EmailClient = c.Client?.Email ?? "—",
            Transporteur = nomT,
            Trajet = c.Trajet is null ? "—" : $"{c.Trajet.VilleDepart} → {c.Trajet.VilleArrivee}",
            DateDepart = c.Trajet?.DateDepart ?? DateTime.UtcNow,
            NomDestinataire = c.NomDestinataire,
            TelephoneDestinataire = c.TelephoneDestinataire,
            VilleDestinataire = c.VilleDestinataire,
            DescriptionContenu = c.DescriptionContenu,
            PoidsDeclare = c.PoidsDeclare,
            Dimensions = c.Dimensions,
            ValeurDeclaree = c.ValeurDeclaree,
            PrixTransport = c.PrixTransport,
            FraisService = c.FraisService,
            SupplementsTotal = c.SupplementsTotal,
            Total = c.Total,
            InstructionsParticulieres = c.InstructionsParticulieres,
            DateCreation = c.DateCreation,
            EstAnnulable = Domain.RulesMetier.Annulation.EstAnnulable(c.Colis?.Statut ?? StatutColis.Brouillon)
        };
    }

    public async Task<IReadOnlyList<PaiementAdminListItem>> GetPaiementsAsync(string? modeFilter, CancellationToken ct = default)
    {
        var query = _db.Paiements
            .Include(p => p.Commande).ThenInclude(c => c!.Client)
            .Include(p => p.Commande).ThenInclude(c => c!.Colis)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(modeFilter) && Enum.TryParse<ModeReglement>(modeFilter, out var mode))
            query = query.Where(p => p.Mode == mode);

        return await query
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

    public async Task<OperationResult> ResoudreLitigeAsync(Guid commandeId, string commentaire, CancellationToken ct = default)
    {
        var commande = await _db.Commandes
            .Include(c => c.Colis)
            .FirstOrDefaultAsync(c => c.Id == commandeId, ct);
        if (commande?.Colis is null) return OperationResult.Fail("Commande/colis introuvable.");

        var ancien = commande.Colis.Statut;
        commande.Colis.Statut = StatutColis.LivraisonCloturee;

        await _db.EvenementsColis.AddAsync(new Domain.Entities.EvenementColis
        {
            ColisId = commande.Colis.Id,
            AncienStatut = ancien,
            NouveauStatut = StatutColis.LivraisonCloturee,
            ActeurId = Guid.Empty,
            Commentaire = $"Litige résolu par admin : {commentaire}"
        }, ct);

        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<IReadOnlyList<UtilisateurListItem>> RechercheGlobaleAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<UtilisateurListItem>();
        var q = query.Trim().ToLower();

        return await _db.Utilisateurs
            .Where(u => u.Email.ToLower().Contains(q) ||
                        u.Nom.ToLower().Contains(q) ||
                        u.Prenom.ToLower().Contains(q))
            .OrderByDescending(u => u.DateCreation)
            .Take(20)
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
}
