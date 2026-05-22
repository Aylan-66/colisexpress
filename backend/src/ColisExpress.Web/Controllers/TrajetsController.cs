using System.Security.Claims;
using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/trajets")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstTransporteur")]
public class TrajetsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ColisExpressDbContext _db;

    public TrajetsController(IUnitOfWork uow, ColisExpressDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTrajets(CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajets = await _uow.Trajets.GetByTransporteurIdAsync(transporteur.Id, ct);
        return Ok(trajets.Select(t => new
        {
            t.Id,
            t.PaysDepart, t.VilleDepart,
            t.PaysArrivee, t.VilleArrivee,
            t.DateDepart, t.DateEstimeeArrivee,
            t.CapaciteMaxPoids, t.NombreMaxColis, t.CapaciteRestante,
            ModeTarification = t.ModeTarification.ToString(),
            t.PrixParColis, t.PrixAuKilo,
            t.SupplementUrgent, t.SupplementFragile,
            t.PointDepot, t.Conditions,
            Statut = t.Statut.ToString(),
            t.DateCreation
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrajetApiRequest request, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();
        if (transporteur.StatutKyc != StatutKyc.Valide)
            return BadRequest(new { error = "Votre KYC doit être validé pour publier un trajet." });

        var trajet = new Trajet
        {
            TransporteurId = transporteur.Id,
            PaysDepart = request.PaysDepart,
            VilleDepart = request.VilleDepart,
            PaysArrivee = request.PaysArrivee,
            VilleArrivee = request.VilleArrivee,
            DateDepart = DateTime.SpecifyKind(request.DateDepart, DateTimeKind.Utc),
            DateEstimeeArrivee = DateTime.SpecifyKind(request.DateEstimeeArrivee, DateTimeKind.Utc),
            CapaciteMaxPoids = request.CapaciteMaxPoids,
            NombreMaxColis = request.NombreMaxColis,
            CapaciteRestante = request.NombreMaxColis,
            ModeTarification = request.ModeTarification,
            PrixParColis = request.PrixParColis,
            PrixAuKilo = request.PrixAuKilo,
            SupplementUrgent = request.SupplementUrgent,
            SupplementFragile = request.SupplementFragile,
            PointDepot = request.PointDepot,
            Conditions = request.Conditions,
            TarifId = request.TarifId,
            RelaisDepartId = request.RelaisDepartId,
            LongueurMaxColisCm = request.LongueurMaxColisCm,
            LargeurMaxColisCm = request.LargeurMaxColisCm,
            HauteurMaxColisCm = request.HauteurMaxColisCm,
            PoidsMaxColisKg = request.PoidsMaxColisKg,
            Statut = StatutTrajet.Actif
        };

        await _uow.Trajets.AddAsync(trajet, ct);
        await _uow.SaveChangesAsync(ct);

        return Created($"/api/trajets/{trajet.Id}", new { trajet.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateTrajetApiRequest request, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        trajet.PaysDepart = request.PaysDepart;
        trajet.VilleDepart = request.VilleDepart;
        trajet.PaysArrivee = request.PaysArrivee;
        trajet.VilleArrivee = request.VilleArrivee;
        trajet.DateDepart = DateTime.SpecifyKind(request.DateDepart, DateTimeKind.Utc);
        trajet.DateEstimeeArrivee = DateTime.SpecifyKind(request.DateEstimeeArrivee, DateTimeKind.Utc);
        trajet.CapaciteMaxPoids = request.CapaciteMaxPoids;
        trajet.NombreMaxColis = request.NombreMaxColis;
        trajet.ModeTarification = request.ModeTarification;
        trajet.PrixParColis = request.PrixParColis;
        trajet.PrixAuKilo = request.PrixAuKilo;
        trajet.SupplementUrgent = request.SupplementUrgent;
        trajet.SupplementFragile = request.SupplementFragile;
        trajet.PointDepot = request.PointDepot;
        trajet.Conditions = request.Conditions;
        trajet.TarifId = request.TarifId;
        trajet.RelaisDepartId = request.RelaisDepartId;
        trajet.LongueurMaxColisCm = request.LongueurMaxColisCm;
        trajet.LargeurMaxColisCm = request.LargeurMaxColisCm;
        trajet.HauteurMaxColisCm = request.HauteurMaxColisCm;
        trajet.PoidsMaxColisKg = request.PoidsMaxColisKg;

        await _uow.SaveChangesAsync(ct);
        return Ok(new { trajet.Id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        _uow.Trajets.Remove(trajet);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/colis")]
    public async Task<IActionResult> GetColisForTrajet(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        var commandes = await _uow.Commandes.GetByTransporteurIdAsync(transporteur.Id, ct);
        var colisTrajet = commandes
            .Where(c => c.TrajetId == id && c.Colis is not null)
            .Select(c => new
            {
                c.Colis!.CodeColis,
                Statut = c.Colis.Statut.ToString(),
                c.NomDestinataire,
                c.TelephoneDestinataire,
                c.VilleDestinataire,
                c.PoidsDeclare,
                c.Total,
                c.DateCreation
            })
            .ToList();

        return Ok(colisTrajet);
    }

    [HttpGet("{id:guid}/etapes/{etapeId:guid}/colis")]
    public async Task<IActionResult> GetColisForEtape(Guid id, Guid etapeId, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var etape = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .FirstOrDefaultAsync(e => e.Id == etapeId && e.TrajetId == id, ct);
        if (etape?.PointRelais is null) return NotFound(new { error = "Étape introuvable." });

        var allEtapes = await _db.EtapesTrajets
            .Where(e => e.TrajetId == id)
            .OrderBy(e => e.Ordre)
            .ToListAsync(ct);

        var isFirst = allEtapes.FirstOrDefault()?.Id == etapeId;
        var isLast = allEtapes.LastOrDefault()?.Id == etapeId;

        var commandes = await _uow.Commandes.GetByTransporteurIdAsync(transporteur.Id, ct);
        var colisTrajet = commandes.Where(c => c.TrajetId == id && c.Colis is not null).ToList();

        var villeEtape = etape.PointRelais.Ville.ToLowerInvariant();

        // À déposer ici = colis dont le SegmentArrivee match cette étape
        var aDeposer = colisTrajet
            .Where(c => (!string.IsNullOrEmpty(c.SegmentArrivee) ? c.SegmentArrivee : c.VilleDestinataire).ToLowerInvariant() == villeEtape)
            .Select(c => new {
                c.Colis!.CodeColis,
                statut = c.Colis.Statut.ToString(),
                c.NomDestinataire,
                c.TelephoneDestinataire,
                c.VilleDestinataire,
                c.PoidsDeclare,
                c.Total,
                action = "deposer"
            }).ToList();

        // À récupérer ici = colis dont le SegmentDepart match cette étape
        var villeTrajetDepart = colisTrajet.FirstOrDefault()?.Trajet?.VilleDepart?.ToLowerInvariant() ?? "";
        var aRecuperer = colisTrajet
            .Where(c => {
                var segDep = !string.IsNullOrEmpty(c.SegmentDepart) ? c.SegmentDepart.ToLowerInvariant() : villeTrajetDepart;
                return segDep == villeEtape;
            })
            .Select(c => new {
                c.Colis!.CodeColis,
                statut = c.Colis.Statut.ToString(),
                c.NomDestinataire,
                c.VilleDestinataire,
                c.PoidsDeclare,
                action = "recuperer"
            }).ToList<object>();

        return Ok(new
        {
            etapeId,
            relais = etape.PointRelais.NomRelais,
            ville = etape.PointRelais.Ville,
            type = isFirst ? "depart" : isLast ? "arrivee" : "intermediaire",
            aDeposer,
            aRecuperer,
            totalDeposer = aDeposer.Count,
            totalRecuperer = aRecuperer.Count
        });
    }

    // ============================================
    // ÉTAPES (FICHE DE TOURNÉE)
    // ============================================

    [HttpGet("relais-disponibles")]
    public async Task<IActionResult> GetRelaisDisponibles(
        [FromQuery] string? pays,
        [FromQuery] string? ville,
        [FromQuery] string? departement,
        [FromQuery] string? region,
        CancellationToken ct)
    {
        var query = _db.PointsRelais.Where(p => p.EstActif);
        if (!string.IsNullOrWhiteSpace(pays))
            query = query.Where(p => p.Pays.ToLower() == pays.ToLower());
        if (!string.IsNullOrWhiteSpace(ville))
            query = query.Where(p => p.Ville.ToLower().Contains(ville.ToLower()));
        if (!string.IsNullOrWhiteSpace(departement))
            query = query.Where(p => p.Departement.ToLower().Contains(departement.ToLower()));
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(p => p.Region.ToLower().Contains(region.ToLower()));

        var relais = await query.OrderBy(p => p.Pays).ThenBy(p => p.Ville).Select(p => new
        {
            p.Id,
            p.NomRelais,
            p.Adresse,
            p.Ville,
            p.Departement,
            p.Region,
            p.Pays,
            p.Telephone,
            joursOuverture = p.JoursOuverture ?? "",
            heureOuverture = p.HeureOuverture.HasValue ? p.HeureOuverture.Value.ToString("HH:mm") : null,
            heureFermeture = p.HeureFermeture.HasValue ? p.HeureFermeture.Value.ToString("HH:mm") : null,
        }).ToListAsync(ct);

        return Ok(relais);
    }

    [HttpGet("{id:guid}/etapes")]
    public async Task<IActionResult> GetEtapes(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        var etapes = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .Where(e => e.TrajetId == id)
            .OrderBy(e => e.Ordre)
            .Select(e => new
            {
                e.Id,
                e.Ordre,
                e.HeureEstimeeArrivee,
                e.HeureReelleArrivee,
                e.RelaisOuvertALArrivee,
                statut = e.Statut.ToString(),
                relais = new
                {
                    e.PointRelais!.Id,
                    e.PointRelais.NomRelais,
                    e.PointRelais.Ville,
                    e.PointRelais.Pays,
                    joursOuverture = e.PointRelais.JoursOuverture ?? "",
                    heureOuverture = e.PointRelais.HeureOuverture.HasValue ? e.PointRelais.HeureOuverture.Value.ToString("HH:mm") : null,
                    heureFermeture = e.PointRelais.HeureFermeture.HasValue ? e.PointRelais.HeureFermeture.Value.ToString("HH:mm") : null,
                }
            })
            .ToListAsync(ct);

        return Ok(etapes);
    }

    [HttpPost("{id:guid}/etapes")]
    public async Task<IActionResult> AddEtape(Guid id, [FromBody] AddEtapeRequest request, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        var relais = await _db.PointsRelais.FirstOrDefaultAsync(p => p.Id == request.PointRelaisId && p.EstActif, ct);
        if (relais is null) return BadRequest(new { error = "Point relais introuvable ou inactif." });

        var heureArrivee = DateTime.SpecifyKind(request.HeureEstimeeArrivee, DateTimeKind.Utc);

        // Vérifier si le relais est ouvert à l'heure estimée
        var jour = heureArrivee.DayOfWeek;
        var heure = TimeOnly.FromDateTime(heureArrivee);
        var ouvert = relais.EstOuvert(jour, heure);

        var maxOrdre = await _db.EtapesTrajets
            .Where(e => e.TrajetId == id)
            .MaxAsync(e => (int?)e.Ordre, ct) ?? 0;

        var etape = new EtapeTrajet
        {
            TrajetId = id,
            PointRelaisId = request.PointRelaisId,
            Ordre = maxOrdre + 1,
            HeureEstimeeArrivee = heureArrivee,
            RelaisOuvertALArrivee = ouvert,
            Statut = StatutEtape.Planifiee
        };

        await _db.EtapesTrajets.AddAsync(etape, ct);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/trajets/{id}/etapes", new
        {
            etape.Id,
            etape.Ordre,
            etape.HeureEstimeeArrivee,
            relaisOuvert = ouvert,
            relaisNom = relais.NomRelais,
            relaisVille = relais.Ville,
            warning = ouvert ? null : $"Attention : {relais.NomRelais} sera fermé à cette heure."
        });
    }

    [HttpDelete("{id:guid}/etapes/{etapeId:guid}")]
    public async Task<IActionResult> RemoveEtape(Guid id, Guid etapeId, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var etape = await _db.EtapesTrajets.FirstOrDefaultAsync(e => e.Id == etapeId && e.TrajetId == id, ct);
        if (etape is null) return NotFound(new { error = "Étape introuvable." });

        _db.EtapesTrajets.Remove(etape);
        await _db.SaveChangesAsync(ct);

        // Réordonner
        var remaining = await _db.EtapesTrajets.Where(e => e.TrajetId == id).OrderBy(e => e.Ordre).ToListAsync(ct);
        for (int i = 0; i < remaining.Count; i++)
            remaining[i].Ordre = i + 1;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/lancer")]
    public async Task<IActionResult> LancerTournee(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        var etapes = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .Where(e => e.TrajetId == id)
            .OrderBy(e => e.Ordre)
            .ToListAsync(ct);

        // Vérifier qu'aucune étape n'a un relais fermé
        var fermees = etapes.Where(e => !e.RelaisOuvertALArrivee).ToList();
        if (fermees.Any())
        {
            var noms = string.Join(", ", fermees.Select(e => e.PointRelais?.NomRelais ?? "?"));
            return BadRequest(new { error = $"Impossible de lancer : relais fermé(s) à l'heure prévue : {noms}. Modifiez les horaires ou supprimez ces étapes." });
        }

        // Marquer la première étape comme en cours
        if (etapes.Any())
            etapes[0].Statut = StatutEtape.EnCours;

        trajet.Statut = StatutTrajet.Actif;
        trajet.DateDemarrageTournee = DateTime.UtcNow;

        // Passer en transit tous les colis pris en charge / déposés du trajet
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var commandes = await _db.Commandes
            .Include(c => c.Colis)
            .Where(c => c.TrajetId == id && c.Colis != null)
            .ToListAsync(ct);
        var enTransitCount = 0;
        foreach (var c in commandes)
        {
            var s = c.Colis!.Statut;
            if (s is StatutColis.DeposeParClient or StatutColis.ReceptionneParTransporteur or StatutColis.PhotoPriseEnChargeEnregistree)
            {
                var ancien = c.Colis.Statut;
                c.Colis.Statut = StatutColis.EnTransit;
                await _uow.Colis.AddEvenementAsync(new EvenementColis
                {
                    ColisId = c.Colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.EnTransit,
                    ActeurId = userId, Commentaire = "Tournée démarrée par le transporteur — colis en transit"
                }, ct);
                enTransitCount++;
            }
        }

        await _db.SaveChangesAsync(ct);

        // TODO notifications : prévenir les clients que la tournée a démarré (en attente SMTP/push)
        return Ok(new { message = "Tournée démarrée.", etapes = etapes.Count, colisEnTransit = enTransitCount });
    }

    /// Suppression en masse de plusieurs trajets (sélection multiple côté app)
    [HttpPost("suppression-masse")]
    public async Task<IActionResult> SuppressionMasse([FromBody] SuppressionMasseRequest body, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();
        if (body.TrajetIds is null || body.TrajetIds.Count == 0)
            return BadRequest(new { error = "Aucun trajet sélectionné." });

        var trajets = await _db.Trajets
            .Where(t => body.TrajetIds.Contains(t.Id) && t.TransporteurId == transporteur.Id)
            .ToListAsync(ct);

        // Refus de supprimer un trajet qui a des colis non terminés
        var trajetIds = trajets.Select(t => t.Id).ToList();
        var trajetsAvecColisActifs = await _db.Commandes
            .Where(c => trajetIds.Contains(c.TrajetId) && c.Colis != null
                && c.Colis.Statut != StatutColis.Annulee
                && c.Colis.Statut != StatutColis.LivraisonCloturee
                && c.Colis.Statut != StatutColis.Refuse)
            .Select(c => c.TrajetId)
            .Distinct()
            .ToListAsync(ct);

        var supprimables = trajets.Where(t => !trajetsAvecColisActifs.Contains(t.Id)).ToList();
        _db.Trajets.RemoveRange(supprimables);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            supprimes = supprimables.Count,
            ignores = trajets.Count - supprimables.Count,
            message = trajetsAvecColisActifs.Any()
                ? $"{supprimables.Count} trajet(s) supprimé(s). {trajetsAvecColisActifs.Count} ignoré(s) car ils ont des colis en cours."
                : $"{supprimables.Count} trajet(s) supprimé(s)."
        });
    }

    [HttpPost("{id:guid}/etapes/{etapeId:guid}/arrivee")]
    public async Task<IActionResult> MarquerArrivee(Guid id, Guid etapeId, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var etape = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .FirstOrDefaultAsync(e => e.Id == etapeId && e.TrajetId == id, ct);
        if (etape is null) return NotFound(new { error = "Étape introuvable." });

        etape.HeureReelleArrivee = DateTime.UtcNow;
        etape.Statut = StatutEtape.Terminee;

        // Activer la prochaine étape
        var next = await _db.EtapesTrajets
            .Where(e => e.TrajetId == id && e.Ordre > etape.Ordre && e.Statut == StatutEtape.Planifiee)
            .OrderBy(e => e.Ordre)
            .FirstOrDefaultAsync(ct);
        if (next is not null)
            next.Statut = StatutEtape.EnCours;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Arrivée à {etape.PointRelais?.NomRelais} confirmée.",
            heureReelle = etape.HeureReelleArrivee,
            prochaineEtape = next is not null ? next.PointRelais?.NomRelais : "Aucune (dernière étape)"
        });
    }

    [HttpPut("{id:guid}/etapes/{etapeId:guid}")]
    public async Task<IActionResult> UpdateEtape(Guid id, Guid etapeId, [FromBody] UpdateEtapeRequest request, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var etape = await _db.EtapesTrajets
            .Include(e => e.PointRelais)
            .FirstOrDefaultAsync(e => e.Id == etapeId && e.TrajetId == id, ct);
        if (etape is null) return NotFound(new { error = "Etape introuvable." });

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        var heureArrivee = DateTime.SpecifyKind(request.HeureEstimeeArrivee, DateTimeKind.Utc);
        etape.HeureEstimeeArrivee = heureArrivee;

        // Re-check if relay is open at the new time
        var ouvert = true;
        if (etape.PointRelais is not null)
        {
            var jour = heureArrivee.DayOfWeek;
            var heure = TimeOnly.FromDateTime(heureArrivee);
            ouvert = etape.PointRelais.EstOuvert(jour, heure);
        }
        etape.RelaisOuvertALArrivee = ouvert;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            etape.Id,
            etape.HeureEstimeeArrivee,
            relaisOuvert = ouvert,
            relaisNom = etape.PointRelais?.NomRelais,
            warning = ouvert ? null : $"Attention : {etape.PointRelais?.NomRelais} sera ferme a cette heure."
        });
    }

    [HttpPost("{id:guid}/demander-annulation")]
    public async Task<IActionResult> DemanderAnnulation(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        if (trajet.Statut == StatutTrajet.Annule)
            return BadRequest(new { error = "Ce trajet est deja annule." });
        if (trajet.Statut == StatutTrajet.EnCoursAnnulation)
            return BadRequest(new { error = "Une demande d'annulation est deja en cours." });

        trajet.Statut = StatutTrajet.EnCoursAnnulation;
        await _uow.SaveChangesAsync(ct);

        // Find commandes that need refunding (paid via Stripe with a session ID)
        var commandes = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Client)
            .Where(c => c.TrajetId == id)
            .ToListAsync(ct);

        var commandeIds = commandes.Select(c => c.Id).ToList();
        var paiements = await _db.Paiements
            .Where(p => commandeIds.Contains(p.CommandeId)
                && p.Mode == ModeReglement.Carte
                && p.ReferenceExterne != null
                && p.ReferenceExterne != ""
                && p.Statut != StatutReglement.Rembourse)
            .ToListAsync(ct);

        var commandesARefund = paiements.Select(p =>
        {
            var cmd = commandes.First(c => c.Id == p.CommandeId);
            return new
            {
                commandeId = cmd.Id,
                codeColis = cmd.Colis?.CodeColis ?? "—",
                clientNom = cmd.Client is null ? "—" : $"{cmd.Client.Prenom} {cmd.Client.Nom}",
                clientEmail = cmd.Client?.Email ?? "—",
                montant = p.Montant,
                stripeSessionId = p.ReferenceExterne
            };
        }).ToList();

        return Ok(new
        {
            message = "Demande d'annulation enregistree. Un admin doit confirmer.",
            trajetId = id,
            commandesARefund
        });
    }

    /// Liste les colis en attente de validation transporteur (après paiement client)
    [HttpGet("/api/transporteur/colis-en-attente")]
    public async Task<IActionResult> GetColisEnAttenteValidation(CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var commandes = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Trajet)
            .Include(c => c.Client)
            .Where(c => c.TransporteurId == transporteur.Id
                     && c.Colis != null
                     && c.Colis.Statut == StatutColis.EnAttenteValidationTransporteur)
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

        return Ok(commandes.Select(c => new
        {
            commandeId = c.Id,
            colisId = c.Colis!.Id,
            codeColis = c.Colis.CodeColis,
            trajetId = c.TrajetId,
            trajet = c.Trajet is null ? "—" : $"{c.Trajet.VilleDepart} → {c.Trajet.VilleArrivee}",
            dateDepart = c.Trajet?.DateDepart,
            segmentDepart = c.SegmentDepart,
            segmentArrivee = c.SegmentArrivee,
            nomDestinataire = c.NomDestinataire,
            telephoneDestinataire = c.TelephoneDestinataire,
            villeDestinataire = c.VilleDestinataire,
            descriptionContenu = c.DescriptionContenu,
            poidsDeclare = c.PoidsDeclare,
            longueurCm = c.LongueurCm,
            largeurCm = c.LargeurCm,
            hauteurCm = c.HauteurCm,
            dimensions = c.Dimensions,
            valeurDeclaree = c.ValeurDeclaree,
            total = c.Total,
            modeReglement = c.ModeReglement.ToString(),
            client = c.Client is null ? "—" : $"{c.Client.Prenom} {c.Client.Nom}",
            clientEmail = c.Client?.Email,
            dateCreation = c.DateCreation
        }));
    }

    /// Valider la prise en charge d'un colis (transporteur uniquement) → passe à EnAttenteDepot
    [HttpPost("/api/transporteur/colis/{codeColis}/valider")]
    public async Task<IActionResult> ValiderColis(string codeColis, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        var commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == colis.CommandeId, ct);
        if (commande is null || commande.TransporteurId != transporteur.Id)
            return Forbid();

        if (colis.Statut != StatutColis.EnAttenteValidationTransporteur)
            return BadRequest(new { error = $"Ce colis n'est pas en attente de validation (statut : {colis.Statut})." });

        var ancien = colis.Statut;
        colis.Statut = StatutColis.EnAttenteDepot;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id, AncienStatut = ancien, NouveauStatut = StatutColis.EnAttenteDepot,
            ActeurId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Commentaire = "Prise en charge validée par le transporteur"
        }, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { message = "Colis validé. Le client peut le déposer au point relais.", statut = colis.Statut.ToString() });
    }

    /// Clôturer un trajet : passe à Termine, n'apparaît plus en recherche
    [HttpPost("{id:guid}/cloturer")]
    public async Task<IActionResult> Cloturer(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        if (trajet.Statut == StatutTrajet.Termine) return Ok(new { message = "Déjà clôturé." });
        trajet.Statut = StatutTrajet.Termine;
        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = "Trajet clôturé.", statut = trajet.Statut.ToString() });
    }

    /// Dupliquer un trajet existant — retourne le nouveau trajet (dates à ajuster côté client)
    [HttpPost("{id:guid}/dupliquer")]
    public async Task<IActionResult> Dupliquer(Guid id, [FromBody] DupliquerTrajetRequest body, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _db.Trajets
            .Include(t => t.Etapes)
            .FirstOrDefaultAsync(t => t.Id == id && t.TransporteurId == transporteur.Id, ct);
        if (trajet is null) return NotFound(new { error = "Trajet introuvable." });

        var decalage = body.NouvelleDateDepart - trajet.DateDepart;
        var nouveau = new Trajet
        {
            TransporteurId = trajet.TransporteurId,
            PaysDepart = trajet.PaysDepart,
            VilleDepart = trajet.VilleDepart,
            PaysArrivee = trajet.PaysArrivee,
            VilleArrivee = trajet.VilleArrivee,
            DateDepart = DateTime.SpecifyKind(body.NouvelleDateDepart, DateTimeKind.Utc),
            DateEstimeeArrivee = DateTime.SpecifyKind(trajet.DateEstimeeArrivee + decalage, DateTimeKind.Utc),
            CapaciteMaxPoids = trajet.CapaciteMaxPoids,
            NombreMaxColis = trajet.NombreMaxColis,
            CapaciteRestante = trajet.NombreMaxColis,
            ModeTarification = trajet.ModeTarification,
            PrixParColis = trajet.PrixParColis,
            PrixAuKilo = trajet.PrixAuKilo,
            SupplementUrgent = trajet.SupplementUrgent,
            SupplementFragile = trajet.SupplementFragile,
            PointDepot = trajet.PointDepot,
            RelaisDepartId = trajet.RelaisDepartId,
            TarifId = trajet.TarifId,
            LongueurMaxColisCm = trajet.LongueurMaxColisCm,
            LargeurMaxColisCm = trajet.LargeurMaxColisCm,
            HauteurMaxColisCm = trajet.HauteurMaxColisCm,
            PoidsMaxColisKg = trajet.PoidsMaxColisKg,
            Conditions = trajet.Conditions,
            Statut = StatutTrajet.Actif
        };
        _db.Trajets.Add(nouveau);
        await _db.SaveChangesAsync(ct);

        // Dupliquer aussi les étapes avec le même décalage
        foreach (var e in trajet.Etapes.OrderBy(e => e.Ordre))
        {
            _db.EtapesTrajets.Add(new EtapeTrajet
            {
                TrajetId = nouveau.Id,
                PointRelaisId = e.PointRelaisId,
                Ordre = e.Ordre,
                HeureEstimeeArrivee = e.HeureEstimeeArrivee + decalage,
                RelaisOuvertALArrivee = e.RelaisOuvertALArrivee,
                Statut = StatutEtape.Planifiee
            });
        }
        await _db.SaveChangesAsync(ct);

        return Created($"/api/trajets/{nouveau.Id}", new { nouveau.Id });
    }

    /// Mettre à jour le statut de TOUS les colis d'un trajet (boutons "En transit", "Arrivé destination", etc.)
    [HttpPost("{id:guid}/colis/batch-statut")]
    public async Task<IActionResult> BatchStatutColis(Guid id, [FromBody] BatchStatutRequest body, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajet = await _uow.Trajets.GetByIdAsync(id, ct);
        if (trajet is null || trajet.TransporteurId != transporteur.Id)
            return NotFound(new { error = "Trajet introuvable." });

        if (!Enum.TryParse<StatutColis>(body.NouveauStatut, out var nouveau))
            return BadRequest(new { error = "Statut invalide." });

        var commandes = await _db.Commandes
            .Include(c => c.Colis)
            .Where(c => c.TrajetId == id && c.Colis != null)
            .ToListAsync(ct);

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var modifies = 0;

        foreach (var c in commandes)
        {
            // On ne touche pas aux colis déjà refusés/livrés/annulés
            if (c.Colis!.Statut is StatutColis.Refuse or StatutColis.LivraisonCloturee
                or StatutColis.RetireParDestinataire or StatutColis.Annulee)
                continue;

            var ancien = c.Colis.Statut;
            c.Colis.Statut = nouveau;
            await _uow.Colis.AddEvenementAsync(new EvenementColis
            {
                ColisId = c.Colis.Id, AncienStatut = ancien, NouveauStatut = nouveau,
                ActeurId = userId,
                Commentaire = $"Mise à jour groupée par le transporteur (trajet {trajet.VilleDepart}→{trajet.VilleArrivee})"
            }, ct);
            modifies++;
        }

        await _uow.SaveChangesAsync(ct);
        return Ok(new { message = $"{modifies} colis mis à jour.", statut = nouveau.ToString() });
    }

    /// Liste les points relais avec lesquels le transporteur travaille (relais de ses trajets actifs)
    [HttpGet("/api/transporteur/relais-partenaires")]
    public async Task<IActionResult> GetRelaisPartenaires(CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var trajets = await _db.Trajets
            .Include(t => t.RelaisDepart)
            .Include(t => t.Etapes).ThenInclude(e => e.PointRelais)
            .Where(t => t.TransporteurId == transporteur.Id && t.Statut != StatutTrajet.Termine)
            .ToListAsync(ct);

        var relaisDict = new Dictionary<Guid, PointRelais>();
        foreach (var t in trajets)
        {
            if (t.RelaisDepart != null) relaisDict[t.RelaisDepart.Id] = t.RelaisDepart;
            foreach (var e in t.Etapes)
                if (e.PointRelais != null) relaisDict[e.PointRelais.Id] = e.PointRelais;
        }

        var relaisIds = relaisDict.Keys.ToList();

        // Colis en attente de récupération côté relais (DisponibleAuRetrait) pour ce transporteur
        var colisEnAttente = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Trajet)
            .Where(c => c.TransporteurId == transporteur.Id
                     && c.Colis != null
                     && c.Colis.Statut == StatutColis.DisponibleAuRetrait)
            .ToListAsync(ct);

        return Ok(relaisDict.Values.Select(r => new
        {
            id = r.Id,
            nom = r.NomRelais,
            adresse = r.Adresse,
            ville = r.Ville,
            pays = r.Pays,
            telephone = r.Telephone,
            colisEnAttente = colisEnAttente
                .Where(c => c.RelaisArriveeId == r.Id || (c.VilleDestinataire?.ToLower() == r.Ville.ToLower()))
                .Select(c => new
                {
                    codeColis = c.Colis!.CodeColis,
                    nomDestinataire = c.NomDestinataire,
                    trajet = c.Trajet is null ? "—" : $"{c.Trajet.VilleDepart} → {c.Trajet.VilleArrivee}"
                }).ToList()
        }));
    }

    private async Task<Transporteur?> GetTransporteurAsync(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _uow.Transporteurs.GetByUtilisateurIdAsync(userId, ct);
    }
}

public class DupliquerTrajetRequest
{
    public DateTime NouvelleDateDepart { get; set; }
}

public class BatchStatutRequest
{
    public string NouveauStatut { get; set; } = string.Empty;
}

public class SuppressionMasseRequest
{
    public List<Guid> TrajetIds { get; set; } = new();
}

public class CreateTrajetApiRequest
{
    public string PaysDepart { get; set; } = string.Empty;
    public string VilleDepart { get; set; } = string.Empty;
    public string PaysArrivee { get; set; } = string.Empty;
    public string VilleArrivee { get; set; } = string.Empty;
    public DateTime DateDepart { get; set; }
    public DateTime DateEstimeeArrivee { get; set; }
    public decimal CapaciteMaxPoids { get; set; }
    public int NombreMaxColis { get; set; }
    public ModeTarification ModeTarification { get; set; }
    public decimal? PrixParColis { get; set; }
    public decimal? PrixAuKilo { get; set; }
    public decimal? SupplementUrgent { get; set; }
    public decimal? SupplementFragile { get; set; }
    public string? PointDepot { get; set; }
    public string? Conditions { get; set; }
    public Guid? TarifId { get; set; }
    public Guid? RelaisDepartId { get; set; }
    public int? LongueurMaxColisCm { get; set; }
    public int? LargeurMaxColisCm { get; set; }
    public int? HauteurMaxColisCm { get; set; }
    public decimal? PoidsMaxColisKg { get; set; }
}

public class AddEtapeRequest
{
    public Guid PointRelaisId { get; set; }
    public DateTime HeureEstimeeArrivee { get; set; }
}

public class UpdateEtapeRequest
{
    public DateTime HeureEstimeeArrivee { get; set; }
}
