using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstAdmin")]
public class AdminApiController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly ColisExpressDbContext _db;

    public AdminApiController(IAdminService admin, ColisExpressDbContext db)
    {
        _admin = admin;
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var data = await _admin.GetDashboardAsync(ct);
        return Ok(data);
    }

    [HttpGet("transporteurs/pending")]
    public async Task<IActionResult> TransporteursPending(CancellationToken ct)
    {
        var all = await _admin.GetTransporteursAsync(ct);
        var pending = all.Where(t => t.StatutKyc == StatutKyc.EnAttente).ToList();
        return Ok(pending);
    }

    [HttpPost("transporteurs/{id:guid}/approve")]
    public async Task<IActionResult> ApproveTransporteur(Guid id, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = id, Approuver = true }, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "KYC approuve." });
    }

    [HttpPost("transporteurs/{id:guid}/reject")]
    public async Task<IActionResult> RejectTransporteur(Guid id, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = id, Approuver = false }, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "KYC rejete." });
    }

    // ============================================
    // ANNULATIONS
    // ============================================

    [HttpGet("annulations")]
    public async Task<IActionResult> GetAnnulations(CancellationToken ct)
    {
        var trajets = await _db.Trajets
            .Include(t => t.Transporteur).ThenInclude(tr => tr!.Utilisateur)
            .Where(t => t.Statut == StatutTrajet.EnCoursAnnulation)
            .ToListAsync(ct);

        var result = new List<object>();

        foreach (var trajet in trajets)
        {
            var commandes = await _db.Commandes
                .Include(c => c.Colis)
                .Include(c => c.Client)
                .Where(c => c.TrajetId == trajet.Id)
                .ToListAsync(ct);

            var commandeIds = commandes.Select(c => c.Id).ToList();
            var paiements = await _db.Paiements
                .Where(p => commandeIds.Contains(p.CommandeId))
                .ToListAsync(ct);

            var commandesInfo = commandes.Select(cmd =>
            {
                var paiement = paiements.FirstOrDefault(p => p.CommandeId == cmd.Id
                    && p.Mode == ModeReglement.Carte
                    && !string.IsNullOrEmpty(p.ReferenceExterne));

                return new
                {
                    commandeId = cmd.Id,
                    codeColis = cmd.Colis?.CodeColis ?? "—",
                    clientNom = cmd.Client is null ? "—" : $"{cmd.Client.Prenom} {cmd.Client.Nom}",
                    clientEmail = cmd.Client?.Email ?? "—",
                    total = cmd.Total,
                    stripeSessionId = paiement?.ReferenceExterne,
                    estStripe = paiement is not null,
                    estRembourse = paiement?.Statut == StatutReglement.Rembourse
                };
            }).ToList();

            var nomTransporteur = trajet.Transporteur?.Utilisateur is not null
                ? $"{trajet.Transporteur.Utilisateur.Prenom} {trajet.Transporteur.Utilisateur.Nom}"
                : "—";

            result.Add(new
            {
                trajetId = trajet.Id,
                transporteur = nomTransporteur,
                trajet = $"{trajet.VilleDepart} -> {trajet.VilleArrivee}",
                dateDepart = trajet.DateDepart,
                commandes = commandesInfo,
                totalCommandes = commandesInfo.Count,
                totalStripeARefund = commandesInfo.Count(c => c.estStripe && !c.estRembourse),
                toutRembourse = commandesInfo.Where(c => c.estStripe).All(c => c.estRembourse)
            });
        }

        return Ok(result);
    }

    [HttpPost("annulations/{trajetId:guid}/confirmer-refund/{commandeId:guid}")]
    public async Task<IActionResult> ConfirmerRefund(Guid trajetId, Guid commandeId, CancellationToken ct)
    {
        var trajet = await _db.Trajets.FirstOrDefaultAsync(t => t.Id == trajetId && t.Statut == StatutTrajet.EnCoursAnnulation, ct);
        if (trajet is null)
            return NotFound(new { error = "Trajet introuvable ou pas en cours d'annulation." });

        var commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == commandeId && c.TrajetId == trajetId, ct);
        if (commande is null)
            return NotFound(new { error = "Commande introuvable pour ce trajet." });

        var paiement = await _db.Paiements
            .FirstOrDefaultAsync(p => p.CommandeId == commandeId
                && p.Mode == ModeReglement.Carte
                && !string.IsNullOrEmpty(p.ReferenceExterne), ct);
        if (paiement is null)
            return BadRequest(new { error = "Aucun paiement Stripe trouve pour cette commande." });

        if (paiement.Statut == StatutReglement.Rembourse)
            return BadRequest(new { error = "Ce paiement est deja marque comme rembourse." });

        paiement.Statut = StatutReglement.Rembourse;
        paiement.DateEncaissement = DateTime.UtcNow;
        commande.StatutReglement = StatutReglement.Rembourse;

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Paiement marque comme rembourse.", commandeId, trajetId });
    }

    [HttpPost("annulations/{trajetId:guid}/cloturer")]
    public async Task<IActionResult> CloturerAnnulation(Guid trajetId, CancellationToken ct)
    {
        var trajet = await _db.Trajets.FirstOrDefaultAsync(t => t.Id == trajetId && t.Statut == StatutTrajet.EnCoursAnnulation, ct);
        if (trajet is null)
            return NotFound(new { error = "Trajet introuvable ou pas en cours d'annulation." });

        // Check all Stripe paiements are refunded
        var commandes = await _db.Commandes
            .Where(c => c.TrajetId == trajetId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var paiementsStripeNonRembourses = await _db.Paiements
            .CountAsync(p => commandes.Contains(p.CommandeId)
                && p.Mode == ModeReglement.Carte
                && !string.IsNullOrEmpty(p.ReferenceExterne)
                && p.Statut != StatutReglement.Rembourse, ct);

        if (paiementsStripeNonRembourses > 0)
            return BadRequest(new
            {
                error = $"Impossible de cloturer : {paiementsStripeNonRembourses} paiement(s) Stripe non rembourse(s).",
                paiementsRestants = paiementsStripeNonRembourses
            });

        trajet.Statut = StatutTrajet.Annule;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Annulation cloturee. Le trajet est desormais annule.", trajetId });
    }
}
