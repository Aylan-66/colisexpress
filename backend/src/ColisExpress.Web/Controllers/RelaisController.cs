using System.Security.Claims;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/relais")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RelaisController : ControllerBase
{
    private readonly ColisExpressDbContext _db;
    private readonly IUnitOfWork _uow;

    public RelaisController(ColisExpressDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    [HttpGet("profil")]
    public async Task<IActionResult> GetProfil(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        return Ok(new
        {
            id = relais.Id,
            nomRelais = relais.NomRelais,
            adresse = relais.Adresse,
            ville = relais.Ville,
            pays = relais.Pays,
            telephone = relais.Telephone,
            estActif = relais.EstActif,
            joursOuverture = relais.JoursOuverture ?? "",
            heureOuverture = relais.HeureOuverture?.ToString("HH:mm"),
            heureFermeture = relais.HeureFermeture?.ToString("HH:mm"),
            heureOuvertureWeekend = relais.HeureOuvertureWeekend?.ToString("HH:mm"),
            heureFermetureWeekend = relais.HeureFermetureWeekend?.ToString("HH:mm"),
            typeCommission = relais.TypeCommission.ToString(),
            montantCommission = relais.MontantCommission
        });
    }

    [HttpPut("profil")]
    public async Task<IActionResult> UpdateProfil([FromBody] UpdateRelaisProfilRequest request, CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        if (!string.IsNullOrWhiteSpace(request.NomRelais)) relais.NomRelais = request.NomRelais.Trim();
        if (!string.IsNullOrWhiteSpace(request.Adresse)) relais.Adresse = request.Adresse.Trim();
        if (!string.IsNullOrWhiteSpace(request.Ville)) relais.Ville = request.Ville.Trim();
        if (!string.IsNullOrWhiteSpace(request.Pays)) relais.Pays = request.Pays.Trim();
        if (!string.IsNullOrWhiteSpace(request.Telephone)) relais.Telephone = request.Telephone.Trim();

        if (request.JoursOuverture is not null) relais.JoursOuverture = request.JoursOuverture;
        if (request.HeureOuverture is not null && TimeOnly.TryParse(request.HeureOuverture, out var ho))
            relais.HeureOuverture = ho;
        if (request.HeureFermeture is not null && TimeOnly.TryParse(request.HeureFermeture, out var hf))
            relais.HeureFermeture = hf;
        if (request.HeureOuvertureWeekend is not null && TimeOnly.TryParse(request.HeureOuvertureWeekend, out var how))
            relais.HeureOuvertureWeekend = how;
        if (request.HeureFermetureWeekend is not null && TimeOnly.TryParse(request.HeureFermetureWeekend, out var hfw))
            relais.HeureFermetureWeekend = hfw;

        if (request.TypeCommission is not null && Enum.TryParse<TypeCommission>(request.TypeCommission, true, out var tc))
            relais.TypeCommission = tc;
        if (request.MontantCommission.HasValue)
            relais.MontantCommission = request.MontantCommission.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Profil mis à jour." });
    }

    [HttpGet("colis")]
    public async Task<IActionResult> GetColis(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        var ville = relais.Ville.ToLower();

        // Tous les colis qui passent par ce relais (départ ou arrivée)
        var allCommandes = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Trajet)
            .Include(c => c.Client)
            .Where(c => c.Colis != null && (
                c.SegmentDepart.ToLower() == ville ||
                c.SegmentArrivee.ToLower() == ville ||
                c.VilleDestinataire.ToLower() == ville
            ))
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

        return Ok(allCommandes.Where(c => c.Colis is not null).Select(c => new
        {
            commandeId = c.Id,
            codeColis = c.Colis!.CodeColis,
            statut = c.Colis.Statut.ToString(),
            nomDestinataire = c.NomDestinataire,
            telephoneDestinataire = c.TelephoneDestinataire,
            villeDestinataire = c.VilleDestinataire,
            codeRetrait = c.Colis.CodeRetrait,
            poidsDeclare = c.PoidsDeclare,
            total = c.Total,
            trajet = c.Trajet is null ? "—" : $"{c.Trajet.VilleDepart} → {c.Trajet.VilleArrivee}",
            client = c.Client is null ? "—" : $"{c.Client.Prenom} {c.Client.Nom}",
            dateCreation = c.DateCreation
        }));
    }

    /// Scan : le relais choisit le mode (depot ou retrait) côté app, le backend valide.
    /// Le relais ne peut scanner qu'un colis qui passe par sa ville (départ, étape ou destination).
    [HttpPost("colis/{codeColis}/scan")]
    public async Task<IActionResult> ScanColis(string codeColis, [FromBody] ScanRequest? body, CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        var commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == colis.CommandeId, ct);
        if (commande is null) return BadRequest(new { error = "Commande introuvable." });

        // Sécurité : le relais doit être sur le parcours du colis (ville de départ, étape ou destination)
        var villeRelais = relais.Ville.ToLower();
        var concerne = (commande.SegmentDepart?.ToLower() == villeRelais)
                    || (commande.SegmentArrivee?.ToLower() == villeRelais)
                    || (commande.VilleDestinataire?.ToLower() == villeRelais);
        if (!concerne)
            return BadRequest(new { error = "Ce colis ne passe pas par votre point relais." });

        var mode = (body?.Mode ?? "auto").ToLowerInvariant();
        var ancien = colis.Statut;

        // Mode "retrait" : seul DisponibleAuRetrait est valide → demander code retrait
        if (mode == "retrait")
        {
            if (colis.Statut != StatutColis.DisponibleAuRetrait)
                return BadRequest(new { error = $"Ce colis n'est pas disponible au retrait (statut : {colis.Statut}).", statut = colis.Statut.ToString() });
            return Ok(new { statut = colis.Statut.ToString(), action = "retrait_requis",
                message = "Demandez le code de retrait (4 chiffres) au destinataire.",
                codeRetraitRequis = true });
        }

        // Mode "depot" : valide pour les statuts d'avant-dépôt côté client OU arrivée transporteur
        if (mode == "depot")
        {
            var depotClientOk = colis.Statut is StatutColis.DemandeCreee
                or StatutColis.ReservationConfirmee
                or StatutColis.CodeColisGenere
                or StatutColis.EnAttenteDepot
                or StatutColis.EnAttenteReglement;
            var receptionRelaisOk = colis.Statut == StatutColis.ArriveDansPaysDest;

            if (!depotClientOk && !receptionRelaisOk)
                return BadRequest(new { error = $"Action dépôt impossible pour ce colis (statut : {colis.Statut}).", statut = colis.Statut.ToString() });
        }

        switch (colis.Statut)
        {
            // Espèces non payées → bloquer
            case StatutColis.EnAttenteReglement:
                if (commande.ModeReglement == ModeReglement.Especes && commande.StatutReglement != StatutReglement.Paye)
                    return BadRequest(new {
                        error = "Paiement espèces non confirmé. Encaissez d'abord.",
                        action = "paiement_requis",
                        commandeId = commande.Id,
                        montant = commande.Total
                    });
                // Si payé entre temps, passer en dépôt
                colis.Statut = StatutColis.DeposeParClient;
                await _uow.Colis.AddEvenementAsync(new EvenementColis
                {
                    ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.DeposeParClient,
                    ActeurId = GetUserId(), Commentaire = "Paiement espèces encaissé + colis déposé"
                }, ct);
                await _uow.SaveChangesAsync(ct);
                return Ok(new { statut = colis.Statut.ToString(), action = "depot_client", message = "Paiement encaissé, dépôt confirmé." });

            // Client dépose au relais départ (déjà payé)
            case StatutColis.DemandeCreee:
            case StatutColis.ReservationConfirmee:
            case StatutColis.CodeColisGenere:
            case StatutColis.EnAttenteDepot:
                colis.Statut = StatutColis.DeposeParClient;
                await _uow.Colis.AddEvenementAsync(new EvenementColis
                {
                    ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.DeposeParClient,
                    ActeurId = GetUserId(), Commentaire = "Colis déposé par le client au point relais"
                }, ct);
                await _uow.SaveChangesAsync(ct);
                return Ok(new { statut = colis.Statut.ToString(), action = "depot_client", message = "Dépôt client confirmé." });

            // Transporteur a déposé au relais destination
            case StatutColis.ArriveDansPaysDest:
                colis.Statut = StatutColis.DisponibleAuRetrait;
                await _uow.Colis.AddEvenementAsync(new EvenementColis
                {
                    ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.DisponibleAuRetrait,
                    ActeurId = GetUserId(), Commentaire = "Colis réceptionné par le point relais, disponible au retrait"
                }, ct);
                await _uow.SaveChangesAsync(ct);
                return Ok(new { statut = colis.Statut.ToString(), action = "reception_relais", message = "Colis réceptionné, disponible au retrait." });

            // Retrait par le destinataire (scan final)
            case StatutColis.DisponibleAuRetrait:
                return Ok(new { statut = colis.Statut.ToString(), action = "retrait_requis",
                    message = "Demandez le code de retrait (4 chiffres) au destinataire.",
                    codeRetraitRequis = true });

            default:
                return BadRequest(new { error = $"Action impossible pour le statut actuel : {colis.Statut}.", statut = colis.Statut.ToString() });
        }
    }

    [HttpPost("colis/{codeColis}/confirmer-depot")]
    public async Task<IActionResult> ConfirmerDepot(string codeColis, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        var commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == colis.CommandeId, ct);
        if (commande is not null && commande.ModeReglement == ModeReglement.Especes && commande.StatutReglement != StatutReglement.Paye)
            return BadRequest(new { error = "Paiement espèces non confirmé.", commandeId = commande.Id });

        var ancien = colis.Statut;
        colis.Statut = StatutColis.DeposeParClient;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.DeposeParClient,
            ActeurId = GetUserId(), Commentaire = "Colis déposé par le client au point relais"
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = colis.Statut.ToString(), message = "Dépôt confirmé." });
    }

    [HttpPost("colis/{codeColis}/confirmer-retrait")]
    public async Task<IActionResult> ConfirmerRetrait(string codeColis, [FromBody] ConfirmerRetraitRequest request, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        if (colis.CodeRetrait != request.CodeRetrait)
            return BadRequest(new { error = "Code de retrait incorrect." });

        var ancien = colis.Statut;
        colis.Statut = StatutColis.RetireParDestinataire;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.RetireParDestinataire,
            ActeurId = GetUserId(), Commentaire = "Colis retiré par le destinataire (code vérifié)"
        }, ct);

        colis.Statut = StatutColis.LivraisonCloturee;
        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id, AncienStatut = StatutColis.RetireParDestinataire, NouveauStatut = StatutColis.LivraisonCloturee,
            ActeurId = GetUserId(), Commentaire = "Livraison clôturée"
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = colis.Statut.ToString(), message = "Retrait confirmé, livraison clôturée." });
    }

    [HttpPost("paiement/{commandeId:guid}/valider-especes")]
    public async Task<IActionResult> ValiderPaiementEspeces(Guid commandeId, CancellationToken ct)
    {
        var commande = await _db.Commandes
            .Include(c => c.Colis)
            .FirstOrDefaultAsync(c => c.Id == commandeId, ct);

        if (commande is null) return NotFound(new { error = "Commande introuvable." });

        if (commande.StatutReglement == StatutReglement.Paye)
            return Ok(new { message = "Déjà payé." });

        commande.StatutReglement = StatutReglement.Paye;

        var relais = await GetRelaisAsync(ct);
        var paiement = new Paiement
        {
            CommandeId = commande.Id,
            Mode = ModeReglement.Especes,
            Montant = commande.Total,
            Statut = StatutReglement.Paye,
            DateEncaissement = DateTime.UtcNow,
            ReferenceExterne = $"Espèces encaissées par relais {GetUserId()}",
            RelaisEncaisseurId = relais?.Id,
            EstReverseAdmin = false
        };
        await _db.Paiements.AddAsync(paiement, ct);

        if (commande.Colis is not null && commande.Colis.Statut == StatutColis.DemandeCreee)
        {
            commande.Colis.Statut = StatutColis.ReservationConfirmee;
            await _uow.Colis.AddEvenementAsync(new EvenementColis
            {
                ColisId = commande.Colis.Id,
                AncienStatut = StatutColis.DemandeCreee,
                NouveauStatut = StatutColis.ReservationConfirmee,
                ActeurId = GetUserId(),
                Commentaire = "Paiement espèces validé par le point relais"
            }, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Paiement espèces validé." });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        var ville = relais.Ville.ToLower();
        var debutMois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var commandesQuery = _db.Commandes
            .Include(c => c.Colis)
            .Where(c => c.Colis != null && (
                c.SegmentDepart.ToLower() == ville ||
                c.SegmentArrivee.ToLower() == ville ||
                c.VilleDestinataire.ToLower() == ville));

        var totalColis = await commandesQuery.CountAsync(ct);
        var enAttenteRetrait = await commandesQuery.CountAsync(c => c.Colis!.Statut == StatutColis.DisponibleAuRetrait, ct);
        var livres = await commandesQuery.CountAsync(c => c.Colis!.Statut == StatutColis.LivraisonCloturee || c.Colis.Statut == StatutColis.RetireParDestinataire, ct);
        var enTransit = await commandesQuery.CountAsync(c => c.Colis!.Statut == StatutColis.EnTransit || c.Colis.Statut == StatutColis.ReceptionneParTransporteur, ct);

        // Espèces encaissées par CE relais
        var paiementsRelais = _db.Paiements.Where(p => p.RelaisEncaisseurId == relais.Id && p.Statut == StatutReglement.Paye);
        var especesTotal = await paiementsRelais.SumAsync(p => (decimal?)p.Montant, ct) ?? 0m;
        var especesDues = await paiementsRelais.Where(p => !p.EstReverseAdmin).SumAsync(p => (decimal?)p.Montant, ct) ?? 0m;
        var especesMois = await paiementsRelais.Where(p => p.DateEncaissement >= debutMois).SumAsync(p => (decimal?)p.Montant, ct) ?? 0m;
        var nbEncaissementsDus = await paiementsRelais.CountAsync(p => !p.EstReverseAdmin, ct);

        // Activité du mois
        var depotsMois = await _db.EvenementsColis
            .CountAsync(e => e.ActeurId == relais.UtilisateurId && e.NouveauStatut == StatutColis.DeposeParClient && e.DateHeure >= debutMois, ct);
        var retraitsMois = await _db.EvenementsColis
            .CountAsync(e => e.ActeurId == relais.UtilisateurId && e.NouveauStatut == StatutColis.RetireParDestinataire && e.DateHeure >= debutMois, ct);

        return Ok(new
        {
            totalColis,
            enAttenteRetrait,
            livres,
            enTransit,
            depotsMois,
            retraitsMois,
            especes = new
            {
                totalEncaisse = especesTotal,
                montantDu = especesDues,
                ceMois = especesMois,
                nbEncaissementsDus
            }
        });
    }

    [HttpGet("especes/historique")]
    public async Task<IActionResult> GetHistoriqueEspeces(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        var paiements = await _db.Paiements
            .Where(p => p.RelaisEncaisseurId == relais.Id && p.Mode == ModeReglement.Especes)
            .OrderByDescending(p => p.DateEncaissement)
            .Take(100)
            .Select(p => new
            {
                id = p.Id,
                commandeId = p.CommandeId,
                montant = p.Montant,
                dateEncaissement = p.DateEncaissement,
                estReverse = p.EstReverseAdmin,
                dateReversement = p.DateReversement
            })
            .ToListAsync(ct);

        return Ok(paiements);
    }

    private async Task<PointRelais?> GetRelaisAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        return await _db.PointsRelais.FirstOrDefaultAsync(p => p.UtilisateurId == userId, ct);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class UpdateRelaisProfilRequest
{
    public string? NomRelais { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? Telephone { get; set; }
    public string? JoursOuverture { get; set; }
    public string? HeureOuverture { get; set; }
    public string? HeureFermeture { get; set; }
    public string? HeureOuvertureWeekend { get; set; }
    public string? HeureFermetureWeekend { get; set; }
    public string? TypeCommission { get; set; }
    public decimal? MontantCommission { get; set; }
}

public class ConfirmerRetraitRequest
{
    public string CodeRetrait { get; set; } = string.Empty;
}

public class ScanRequest
{
    public string? Mode { get; set; }  // "depot" ou "retrait"
}
