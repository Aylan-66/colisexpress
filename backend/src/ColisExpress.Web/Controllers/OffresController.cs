using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/offres")]
[ApiController]
public class OffresController : ControllerBase
{
    private readonly IRechercheService _recherche;

    public OffresController(IRechercheService recherche) => _recherche = recherche;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string depart,
        [FromQuery] string destination,
        [FromQuery] DateTime? date,
        [FromQuery] decimal poids = 10,
        [FromQuery] string tri = "Prix",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(depart) || string.IsNullOrWhiteSpace(destination))
            return BadRequest(new { error = "depart et destination sont obligatoires." });

        Enum.TryParse<TriOffres>(tri, true, out var triEnum);

        var offres = await _recherche.RechercherAsync(new RechercheOffreRequest
        {
            VilleDepart = depart,
            VilleArrivee = destination,
            DateDepart = date ?? DateTime.UtcNow,
            Poids = poids <= 0 ? 1 : poids,
            Tri = triEnum
        }, ct);

        return Ok(offres);
    }

    [HttpGet("villes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVilles(CancellationToken ct)
    {
        var (depart, arrivee) = await _recherche.GetVillesDispoAsync(ct);
        return Ok(new { depart, arrivee });
    }
}
