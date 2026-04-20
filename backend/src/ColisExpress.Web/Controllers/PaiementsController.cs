using System.Security.Claims;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ColisExpress.Web.Controllers;

[Route("api/paiements")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaiementsController : ControllerBase
{
    private readonly ICommandeService _commande;
    private readonly IStripeService _stripe;

    public PaiementsController(ICommandeService commande, IStripeService stripe)
    {
        _commande = commande;
        _stripe = stripe;
    }

    [HttpPost("{commandeId:guid}/declarer")]
    public async Task<IActionResult> DeclarerPaiement(Guid commandeId, [FromBody] DeclarerPaiementRequest request, CancellationToken ct)
    {
        var clientId = GetUserId();

        try
        {
            await _commande.ConfirmerPaiementAsync(commandeId, clientId, $"Espèces/Chèque déclaré: {request.Mode}", ct);
            return Ok(new { message = "Paiement déclaré avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{commandeId:guid}/stripe")]
    public async Task<IActionResult> CreateStripeSession(Guid commandeId, CancellationToken ct)
    {
        var clientId = GetUserId();
        var commande = await _commande.GetByIdAsync(commandeId, clientId, ct);
        if (commande is null) return NotFound(new { error = "Commande introuvable." });

        try
        {
            var clientEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var session = await _stripe.CreateCheckoutSessionAsync(
                commande.Id,
                commande.CodeColis,
                commande.Total,
                clientEmail,
                $"colisexpress://payment/success?commandeId={commandeId}&session_id={{CHECKOUT_SESSION_ID}}",
                $"colisexpress://payment/cancel?commandeId={commandeId}",
                ct);

            return Ok(new
            {
                sessionId = session.SessionId,
                url = session.Url
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Erreur Stripe : {ex.Message}" });
        }
    }

    [HttpPost("{commandeId:guid}/stripe/confirm")]
    public async Task<IActionResult> ConfirmStripeSession(Guid commandeId, [FromBody] ConfirmStripeRequest request, CancellationToken ct)
    {
        var clientId = GetUserId();
        var paye = await _stripe.EstSessionPayeeAsync(request.SessionId, ct);
        if (!paye) return BadRequest(new { error = "Le paiement n'a pas été confirmé par Stripe." });

        try
        {
            await _commande.ConfirmerPaiementAsync(commandeId, clientId, request.SessionId, ct);
            var commande = await _commande.GetByIdAsync(commandeId, clientId, ct);
            return Ok(new
            {
                message = "Paiement confirmé.",
                codeColis = commande?.CodeColis
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class DeclarerPaiementRequest
{
    public string Mode { get; set; } = "Especes";
}

public class ConfirmStripeRequest
{
    public string SessionId { get; set; } = string.Empty;
}
