using System.Security.Claims;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/colis")]
[ApiController]
public class ColisController : ControllerBase
{
    private readonly IColisService _colis;
    private readonly IUnitOfWork _uow;

    public ColisController(IColisService colis, IUnitOfWork uow)
    {
        _colis = colis;
        _uow = uow;
    }

    [HttpGet("{codeColis}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCode(string codeColis, CancellationToken ct)
    {
        var detail = await _colis.GetByCodeAsync(codeColis, ct);
        if (detail is null) return NotFound(new { error = "Colis introuvable." });
        return Ok(detail);
    }

    [HttpPut("{codeColis}/statut")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateStatut(string codeColis, [FromBody] UpdateStatutApiRequest request, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        if (!Enum.TryParse<StatutColis>(request.NouveauStatut, true, out var nouveauStatut))
            return BadRequest(new { error = $"Statut invalide : {request.NouveauStatut}" });

        var ancien = colis.Statut;
        colis.Statut = nouveauStatut;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = ancien,
            NouveauStatut = nouveauStatut,
            ActeurId = userId,
            Commentaire = request.Commentaire,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = nouveauStatut.ToString() });
    }

    [HttpPost("{codeColis}/photo")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UploadPhoto(string codeColis, IFormFile photo, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        if (photo is null || photo.Length == 0)
            return BadRequest(new { error = "Aucune photo fournie." });

        if (photo.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Photo trop volumineuse (5 Mo max)." });

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms, ct);
        var photoBase64 = Convert.ToBase64String(ms.ToArray());

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ancien = colis.Statut;

        if (colis.Statut == StatutColis.ReceptionneParTransporteur)
            colis.Statut = StatutColis.PhotoPriseEnChargeEnregistree;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = ancien,
            NouveauStatut = colis.Statut,
            ActeurId = userId,
            Commentaire = "Photo de prise en charge enregistrée",
            PhotoChemin = $"data:{photo.ContentType};base64,{photoBase64}"
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = colis.Statut.ToString(), message = "Photo enregistrée." });
    }
}

public class UpdateStatutApiRequest
{
    public string NouveauStatut { get; set; } = string.Empty;
    public string? Commentaire { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
