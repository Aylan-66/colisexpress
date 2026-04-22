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

        // À déposer ici = colis dont la VilleDestinataire match cette étape
        var aDeposer = colisTrajet
            .Where(c => c.VilleDestinataire.ToLowerInvariant() == villeEtape)
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

        // À récupérer ici = colis dont le trajet passe par cette ville en départ
        // Pour la première étape: tous les colis du trajet (point de départ)
        // Pour les intermédiaires: colis qui commencent à cette ville (via le trajet)
        var aRecuperer = new List<object>();
        if (isFirst)
        {
            aRecuperer = colisTrajet
                .Where(c => c.VilleDestinataire.ToLowerInvariant() != villeEtape)
                .Select(c => (object)new {
                    c.Colis!.CodeColis,
                    statut = c.Colis.Statut.ToString(),
                    c.NomDestinataire,
                    c.VilleDestinataire,
                    c.PoidsDeclare,
                    action = "recuperer"
                }).ToList();
        }

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
    public async Task<IActionResult> GetRelaisDisponibles([FromQuery] string? pays, [FromQuery] string? ville, CancellationToken ct)
    {
        var query = _db.PointsRelais.Where(p => p.EstActif);
        if (!string.IsNullOrWhiteSpace(pays))
            query = query.Where(p => p.Pays.ToLower() == pays.ToLower());
        if (!string.IsNullOrWhiteSpace(ville))
            query = query.Where(p => p.Ville.ToLower().Contains(ville.ToLower()));

        var relais = await query.OrderBy(p => p.Pays).ThenBy(p => p.Ville).Select(p => new
        {
            p.Id,
            p.NomRelais,
            p.Adresse,
            p.Ville,
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
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Tournée lancée.", etapes = etapes.Count });
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

    private async Task<Transporteur?> GetTransporteurAsync(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _uow.Transporteurs.GetByUtilisateurIdAsync(userId, ct);
    }
}

public class CreateTrajetApiRequest
{
    public string PaysDepart { get; set; } = "France";
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
}

public class AddEtapeRequest
{
    public Guid PointRelaisId { get; set; }
    public DateTime HeureEstimeeArrivee { get; set; }
}
