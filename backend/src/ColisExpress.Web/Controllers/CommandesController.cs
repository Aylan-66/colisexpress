using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/commandes")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CommandesController : ControllerBase
{
    private readonly ICommandeService _commande;

    public CommandesController(ICommandeService commande) => _commande = commande;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommandeRequest request, CancellationToken ct)
    {
        request.ClientId = GetUserId();

        try
        {
            var response = await _commande.CreateAsync(request, ct);
            return Created($"/api/commandes/{response.Id}", response);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCommandes([FromQuery] string? filtre, CancellationToken ct)
    {
        Enum.TryParse<FiltreCommandes>(filtre, true, out var filtreEnum);
        var commandes = await _commande.GetCommandesClientAsync(GetUserId(), filtreEnum, ct);
        return Ok(commandes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var detail = await _commande.GetDetailAsync(id, GetUserId(), ct);
        if (detail is null) return NotFound(new { error = "Commande introuvable." });
        return Ok(detail);
    }

    [HttpPost("{id:guid}/annuler")]
    public async Task<IActionResult> Annuler(Guid id, CancellationToken ct)
    {
        var result = await _commande.AnnulerAsync(id, GetUserId(), ct);
        if (!result.Success) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Commande annulée." });
    }

    [HttpGet("transporteur")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EstTransporteur")]
    public async Task<IActionResult> GetCommandesTransporteur(CancellationToken ct)
    {
        var commandes = await _commande.GetCommandesTransporteurAsync(GetUserId(), ct);
        return Ok(commandes);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
