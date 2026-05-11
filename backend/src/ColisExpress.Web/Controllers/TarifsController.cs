using System.Security.Claims;
using ColisExpress.Domain.Entities;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/tarifs")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstTransporteur")]
public class TarifsController : ControllerBase
{
    private readonly ColisExpressDbContext _db;
    public TarifsController(ColisExpressDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMyTarifs(CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var tarifs = await _db.Tarifs
            .Where(t => t.TransporteurId == transporteur.Id)
            .OrderByDescending(t => t.EstActif).ThenBy(t => t.Nom)
            .ToListAsync(ct);

        return Ok(tarifs.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var tarif = await _db.Tarifs.FirstOrDefaultAsync(t => t.Id == id && t.TransporteurId == transporteur.Id, ct);
        return tarif is null ? NotFound(new { error = "Tarif introuvable." }) : Ok(Map(tarif));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TarifRequest body, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var err = Validate(body);
        if (err != null) return BadRequest(new { error = err });

        var tarif = new Tarif
        {
            TransporteurId = transporteur.Id,
            Nom = body.Nom!.Trim(),
            Description = body.Description?.Trim(),
            PrixAuKiloStandard = body.PrixAuKiloStandard,
            SeuilStandardKg = body.SeuilStandardKg,
            ForfaitLourd = body.ForfaitLourd,
            PrixAuKiloLourd = body.PrixAuKiloLourd,
            ForfaitHorsGabarit = body.ForfaitHorsGabarit,
            PrixAuKiloHorsGabarit = body.PrixAuKiloHorsGabarit,
            LongueurMaxStandardCm = body.LongueurMaxStandardCm,
            LargeurMaxStandardCm = body.LargeurMaxStandardCm,
            HauteurMaxStandardCm = body.HauteurMaxStandardCm,
            EstActif = true
        };
        _db.Tarifs.Add(tarif);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/tarifs/{tarif.Id}", Map(tarif));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TarifRequest body, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var tarif = await _db.Tarifs.FirstOrDefaultAsync(t => t.Id == id && t.TransporteurId == transporteur.Id, ct);
        if (tarif is null) return NotFound(new { error = "Tarif introuvable." });

        var err = Validate(body);
        if (err != null) return BadRequest(new { error = err });

        tarif.Nom = body.Nom!.Trim();
        tarif.Description = body.Description?.Trim();
        tarif.PrixAuKiloStandard = body.PrixAuKiloStandard;
        tarif.SeuilStandardKg = body.SeuilStandardKg;
        tarif.ForfaitLourd = body.ForfaitLourd;
        tarif.PrixAuKiloLourd = body.PrixAuKiloLourd;
        tarif.ForfaitHorsGabarit = body.ForfaitHorsGabarit;
        tarif.PrixAuKiloHorsGabarit = body.PrixAuKiloHorsGabarit;
        tarif.LongueurMaxStandardCm = body.LongueurMaxStandardCm;
        tarif.LargeurMaxStandardCm = body.LargeurMaxStandardCm;
        tarif.HauteurMaxStandardCm = body.HauteurMaxStandardCm;
        if (body.EstActif.HasValue) tarif.EstActif = body.EstActif.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(Map(tarif));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var transporteur = await GetTransporteurAsync(ct);
        if (transporteur is null) return Forbid();

        var tarif = await _db.Tarifs.FirstOrDefaultAsync(t => t.Id == id && t.TransporteurId == transporteur.Id, ct);
        if (tarif is null) return NotFound(new { error = "Tarif introuvable." });

        // Soft delete : juste désactivation (un trajet peut encore le référencer)
        tarif.EstActif = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("simuler")]
    public IActionResult Simuler([FromBody] SimulationRequest body)
    {
        var tarif = new Tarif
        {
            PrixAuKiloStandard = body.PrixAuKiloStandard,
            SeuilStandardKg = body.SeuilStandardKg,
            ForfaitLourd = body.ForfaitLourd,
            PrixAuKiloLourd = body.PrixAuKiloLourd,
            ForfaitHorsGabarit = body.ForfaitHorsGabarit,
            PrixAuKiloHorsGabarit = body.PrixAuKiloHorsGabarit,
            LongueurMaxStandardCm = body.LongueurMaxStandardCm,
            LargeurMaxStandardCm = body.LargeurMaxStandardCm,
            HauteurMaxStandardCm = body.HauteurMaxStandardCm
        };
        var prix = tarif.CalculerPrix(body.PoidsKg, body.LongueurCm, body.LargeurCm, body.HauteurCm);
        return Ok(new { prix });
    }

    private static string? Validate(TarifRequest b)
    {
        if (string.IsNullOrWhiteSpace(b.Nom)) return "Nom obligatoire.";
        if (b.PrixAuKiloStandard < 0 || b.SeuilStandardKg <= 0) return "Tarif standard invalide.";
        if (b.ForfaitLourd < 0 || b.PrixAuKiloLourd < 0) return "Tarif lourd invalide.";
        if (b.ForfaitHorsGabarit < 0 || b.PrixAuKiloHorsGabarit < 0) return "Tarif hors gabarit invalide.";
        if (b.LongueurMaxStandardCm <= 0 || b.LargeurMaxStandardCm <= 0 || b.HauteurMaxStandardCm <= 0)
            return "Limites standard invalides.";
        return null;
    }

    private static object Map(Tarif t) => new
    {
        t.Id, t.Nom, t.Description,
        t.PrixAuKiloStandard, t.SeuilStandardKg,
        t.ForfaitLourd, t.PrixAuKiloLourd,
        t.ForfaitHorsGabarit, t.PrixAuKiloHorsGabarit,
        t.LongueurMaxStandardCm, t.LargeurMaxStandardCm, t.HauteurMaxStandardCm,
        t.EstActif, t.DateCreation
    };

    private async Task<Transporteur?> GetTransporteurAsync(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Transporteurs.FirstOrDefaultAsync(t => t.UtilisateurId == userId, ct);
    }
}

public class TarifRequest
{
    public string? Nom { get; set; }
    public string? Description { get; set; }
    public decimal PrixAuKiloStandard { get; set; }
    public decimal SeuilStandardKg { get; set; }
    public decimal ForfaitLourd { get; set; }
    public decimal PrixAuKiloLourd { get; set; }
    public decimal ForfaitHorsGabarit { get; set; }
    public decimal PrixAuKiloHorsGabarit { get; set; }
    public int LongueurMaxStandardCm { get; set; }
    public int LargeurMaxStandardCm { get; set; }
    public int HauteurMaxStandardCm { get; set; }
    public bool? EstActif { get; set; }
}

public class SimulationRequest
{
    public decimal PoidsKg { get; set; }
    public int? LongueurCm { get; set; }
    public int? LargeurCm { get; set; }
    public int? HauteurCm { get; set; }
    public decimal PrixAuKiloStandard { get; set; }
    public decimal SeuilStandardKg { get; set; }
    public decimal ForfaitLourd { get; set; }
    public decimal PrixAuKiloLourd { get; set; }
    public decimal ForfaitHorsGabarit { get; set; }
    public decimal PrixAuKiloHorsGabarit { get; set; }
    public int LongueurMaxStandardCm { get; set; }
    public int LargeurMaxStandardCm { get; set; }
    public int HauteurMaxStandardCm { get; set; }
}
