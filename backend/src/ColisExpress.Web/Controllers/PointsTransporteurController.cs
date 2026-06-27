using System.Security.Claims;
using ColisExpress.Domain.Entities;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/transporteur/points")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstTransporteur")]
public class PointsTransporteurController : ControllerBase
{
    private readonly ColisExpressDbContext _db;
    public PointsTransporteurController(ColisExpressDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var t = await GetTransporteurAsync(ct);
        if (t is null) return Forbid();

        var pts = await _db.PointsTransporteur
            .Where(p => p.TransporteurId == t.Id)
            .OrderBy(p => p.Ville).ThenBy(p => p.Nom)
            .ToListAsync(ct);
        return Ok(pts.Select(Map));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PointTransporteurRequest body, CancellationToken ct)
    {
        var t = await GetTransporteurAsync(ct);
        if (t is null) return Forbid();

        var err = Validate(body);
        if (err != null) return BadRequest(new { error = err });

        var p = new PointTransporteur
        {
            TransporteurId = t.Id,
            Nom = body.Nom!.Trim(),
            Adresse = body.Adresse!.Trim(),
            CodePostal = body.CodePostal?.Trim(),
            Ville = body.Ville!.Trim(),
            Pays = string.IsNullOrWhiteSpace(body.Pays) ? "France" : body.Pays!.Trim(),
            Telephone = body.Telephone?.Trim(),
            Horaires = body.Horaires?.Trim(),
            Instructions = body.Instructions?.Trim(),
            Latitude = body.Latitude,
            Longitude = body.Longitude
        };
        _db.PointsTransporteur.Add(p);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/transporteur/points/{p.Id}", Map(p));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PointTransporteurRequest body, CancellationToken ct)
    {
        var t = await GetTransporteurAsync(ct);
        if (t is null) return Forbid();

        var p = await _db.PointsTransporteur.FirstOrDefaultAsync(x => x.Id == id && x.TransporteurId == t.Id, ct);
        if (p is null) return NotFound(new { error = "Point introuvable." });

        var err = Validate(body);
        if (err != null) return BadRequest(new { error = err });

        p.Nom = body.Nom!.Trim();
        p.Adresse = body.Adresse!.Trim();
        p.CodePostal = body.CodePostal?.Trim();
        p.Ville = body.Ville!.Trim();
        p.Pays = string.IsNullOrWhiteSpace(body.Pays) ? "France" : body.Pays!.Trim();
        p.Telephone = body.Telephone?.Trim();
        p.Horaires = body.Horaires?.Trim();
        p.Instructions = body.Instructions?.Trim();
        p.Latitude = body.Latitude;
        p.Longitude = body.Longitude;
        await _db.SaveChangesAsync(ct);
        return Ok(Map(p));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var t = await GetTransporteurAsync(ct);
        if (t is null) return Forbid();

        var p = await _db.PointsTransporteur.FirstOrDefaultAsync(x => x.Id == id && x.TransporteurId == t.Id, ct);
        if (p is null) return NotFound(new { error = "Point introuvable." });
        _db.PointsTransporteur.Remove(p);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Validate(PointTransporteurRequest b)
    {
        if (string.IsNullOrWhiteSpace(b.Nom)) return "Nom obligatoire.";
        if (string.IsNullOrWhiteSpace(b.Adresse)) return "Adresse obligatoire.";
        if (string.IsNullOrWhiteSpace(b.Ville)) return "Ville obligatoire.";
        return null;
    }

    private static object Map(PointTransporteur p) => new
    {
        p.Id, p.Nom, p.Adresse, p.CodePostal, p.Ville, p.Pays, p.Telephone,
        p.Horaires, p.Instructions,
        p.Latitude, p.Longitude, p.DateCreation
    };

    private async Task<Transporteur?> GetTransporteurAsync(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Transporteurs.FirstOrDefaultAsync(t => t.UtilisateurId == userId, ct);
    }
}

public class PointTransporteurRequest
{
    public string? Nom { get; set; }
    public string? Adresse { get; set; }
    public string? CodePostal { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? Telephone { get; set; }
    public string? Horaires { get; set; }
    public string? Instructions { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
