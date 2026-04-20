using System.Security.Claims;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/kyc")]
[ApiController]
public class KycController : ControllerBase
{
    private readonly ITransporteurService _transporteur;

    public KycController(ITransporteurService transporteur) => _transporteur = transporteur;

    [HttpGet("document/{id:guid}")]
    [Authorize(Policy = "EstAdmin")]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken ct)
    {
        var (contenu, contentType, nomFichier) = await _transporteur.GetDocumentFileAsync(id, ct);
        if (contenu is null) return NotFound();
        return File(contenu, contentType ?? "application/octet-stream", nomFichier);
    }

    [HttpGet("status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _transporteur.GetDashboardAsync(userId, ct);
        if (dashboard is null) return NotFound(new { error = "Profil transporteur introuvable." });
        return Ok(new
        {
            transporteurId = dashboard.TransporteurId,
            statutKyc = dashboard.StatutKyc.ToString(),
            peutPublierOffres = dashboard.PeutPublierOffres,
            documents = dashboard.Documents.Select(d => new
            {
                id = d.Id,
                typeDocument = d.TypeDocument.ToString(),
                nomFichier = d.NomFichier,
                statut = d.Statut.ToString(),
                dateSoumission = d.DateSoumission
            })
        });
    }

    [HttpPost("upload")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Upload(
        IFormFile fichier,
        [FromForm] string typeDocument,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _transporteur.GetDashboardAsync(userId, ct);
        if (dashboard is null) return NotFound(new { error = "Profil transporteur introuvable." });

        if (fichier is null || fichier.Length == 0)
            return BadRequest(new { error = "Aucun fichier fourni." });

        if (!Enum.TryParse<TypeDocument>(typeDocument, true, out var type))
            return BadRequest(new { error = $"Type de document invalide : {typeDocument}" });

        using var ms = new MemoryStream();
        await fichier.CopyToAsync(ms, ct);

        var result = await _transporteur.UploadDocumentAsync(new UploadDocumentKycRequest
        {
            TransporteurId = dashboard.TransporteurId,
            TypeDocument = type,
            NomFichier = fichier.FileName,
            ContentType = fichier.ContentType,
            Contenu = ms.ToArray()
        }, ct);

        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Document soumis avec succès." });
    }
}
