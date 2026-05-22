using System.Security.Claims;
using ColisExpress.Domain.Entities;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

/// <summary>Toggle des favoris client (auth cookie, appelé en AJAX depuis la page Résultats).</summary>
[Route("favoris")]
[Authorize(Policy = "EstConnecte")]
public class FavorisController : Controller
{
    private readonly ColisExpressDbContext _db;
    public FavorisController(ColisExpressDbContext db) => _db = db;

    [HttpPost("toggle-relais/{relaisId:guid}")]
    public async Task<IActionResult> ToggleRelais(Guid relaisId, CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();

        var existant = await _db.Favoris.FirstOrDefaultAsync(f => f.ClientId == clientId && f.PointRelaisId == relaisId, ct);
        bool favori;
        if (existant is not null) { _db.Favoris.Remove(existant); favori = false; }
        else { _db.Favoris.Add(new Favori { ClientId = clientId.Value, PointRelaisId = relaisId }); favori = true; }
        await _db.SaveChangesAsync(ct);
        return Json(new { favori });
    }

    [HttpPost("toggle-transporteur/{transporteurId:guid}")]
    public async Task<IActionResult> ToggleTransporteur(Guid transporteurId, CancellationToken ct)
    {
        var clientId = GetUserId();
        if (clientId is null) return Unauthorized();

        var existant = await _db.Favoris.FirstOrDefaultAsync(f => f.ClientId == clientId && f.TransporteurId == transporteurId, ct);
        bool favori;
        if (existant is not null) { _db.Favoris.Remove(existant); favori = false; }
        else { _db.Favoris.Add(new Favori { ClientId = clientId.Value, TransporteurId = transporteurId }); favori = true; }
        await _db.SaveChangesAsync(ct);
        return Json(new { favori });
    }

    private Guid? GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
