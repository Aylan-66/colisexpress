using System.Security.Claims;
using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/trajets")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstTransporteur")]
public class TrajetsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public TrajetsController(IUnitOfWork uow) => _uow = uow;

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
