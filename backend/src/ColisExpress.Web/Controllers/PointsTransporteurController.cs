using System.Net.Http;
using System.Net.Http.Json;
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
    private readonly IHttpClientFactory _httpFactory;
    public PointsTransporteurController(ColisExpressDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    /// Géocode une adresse via Nominatim (OpenStreetMap, gratuit, sans clé API).
    /// Cascade : (1) adresse complète → (2) CP + Ville + Pays → (3) Ville + Pays.
    /// Retourne (lat, lng) ou null si vraiment rien trouvé.
    private async Task<(double Lat, double Lng)?> GeocodeAsync(string adresse, string? codePostal, string ville, string pays, CancellationToken ct)
    {
        var tentatives = new[]
        {
            // 1. Adresse complète (la plus précise)
            string.Join(", ", new[] { adresse, codePostal, ville, pays }.Where(s => !string.IsNullOrWhiteSpace(s))),
            // 2. CP + Ville + Pays (fallback : centre du quartier / ville)
            string.Join(", ", new[] { codePostal, ville, pays }.Where(s => !string.IsNullOrWhiteSpace(s))),
            // 3. Ville + Pays seul (centre ville)
            string.Join(", ", new[] { ville, pays }.Where(s => !string.IsNullOrWhiteSpace(s))),
        };

        foreach (var query in tentatives)
        {
            if (string.IsNullOrWhiteSpace(query)) continue;
            var coords = await TryGeocodeAsync(query, ct);
            if (coords.HasValue) return coords;
        }
        return null;
    }

    private async Task<(double Lat, double Lng)?> TryGeocodeAsync(string query, CancellationToken ct)
    {
        try
        {
            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ColisExpress/1.0 (geocoding)");
            http.Timeout = TimeSpan.FromSeconds(5);

            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(query)}";
            var res = await http.GetFromJsonAsync<NominatimResult[]>(url, ct);
            if (res is null || res.Length == 0) return null;
            if (double.TryParse(res[0].lat, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(res[0].lon, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }
            return null;
        }
        catch { return null; }
    }

    private record NominatimResult(string lat, string lon);

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
        // Géocodage best-effort si pas de coords fournies
        if (!p.Latitude.HasValue || !p.Longitude.HasValue)
        {
            var coords = await GeocodeAsync(p.Adresse, p.CodePostal, p.Ville, p.Pays, ct);
            if (coords.HasValue) { p.Latitude = coords.Value.Lat; p.Longitude = coords.Value.Lng; }
        }
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

        var ancienneAdresse = $"{p.Adresse}|{p.CodePostal}|{p.Ville}|{p.Pays}";
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

        // Géocodage best-effort si adresse modifiée ou si coords absentes
        var nouvelleAdresse = $"{p.Adresse}|{p.CodePostal}|{p.Ville}|{p.Pays}";
        if (!p.Latitude.HasValue || !p.Longitude.HasValue || ancienneAdresse != nouvelleAdresse)
        {
            var coords = await GeocodeAsync(p.Adresse, p.CodePostal, p.Ville, p.Pays, ct);
            if (coords.HasValue) { p.Latitude = coords.Value.Lat; p.Longitude = coords.Value.Lng; }
        }
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
