using ColisExpress.Application.Interfaces;
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
}
