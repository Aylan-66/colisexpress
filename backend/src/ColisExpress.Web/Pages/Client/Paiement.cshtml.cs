using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class PaiementModel : PageModel
{
    private readonly ICommandeService _commande;
    private readonly IStripeService _stripe;

    public PaiementModel(ICommandeService commande, IStripeService stripe)
    {
        _commande = commande;
        _stripe = stripe;
    }

    [BindProperty(SupportsGet = true)] public Guid CommandeId { get; set; }
    [BindProperty(SupportsGet = true, Name = "session_id")] public string? SessionId { get; set; }
    [BindProperty(SupportsGet = true)] public bool Annule { get; set; }

    public CommandeResponse? Commande { get; private set; }
    public string? Error { get; set; }
    public string? Info { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        if (Commande is null) return NotFound();

        if (Annule)
            Info = "Le paiement a été annulé. Vous pouvez réessayer.";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string mode, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        if (Commande is null) return NotFound();

        if (string.Equals(mode, "Carte", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{baseUrl}/paiement/{CommandeId}/succes?session_id={{CHECKOUT_SESSION_ID}}";
                var cancelUrl = $"{baseUrl}/paiement/{CommandeId}?annule=true";

                var clientEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

                var session = await _stripe.CreateCheckoutSessionAsync(
                    Commande.Id,
                    Commande.CodeColis,
                    Commande.Total,
                    clientEmail,
                    successUrl,
                    cancelUrl,
                    ct);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                Error = $"Erreur Stripe : {ex.Message}";
                return Page();
            }
        }

        // Espèces ou Chèque → ne PAS confirmer le paiement, juste noter le mode
        // Le relais confirmera le paiement au scan
        try
        {
            await _commande.SetModeReglementAsync(CommandeId, clientId.Value, mode, ct);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }

        var maj = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        return RedirectToPage("/Client/Confirmation", new { codeColis = maj?.CodeColis });
    }

    public async Task<IActionResult> OnGetSuccesAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();
        if (string.IsNullOrWhiteSpace(SessionId)) return BadRequest("session_id manquant");

        var paye = await _stripe.EstSessionPayeeAsync(SessionId, ct);
        if (!paye)
        {
            Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
            Error = "Le paiement n'a pas été confirmé par Stripe.";
            return Page();
        }

        try
        {
            await _commande.ConfirmerPaiementAsync(CommandeId, clientId.Value, SessionId, ct);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
            return Page();
        }

        var maj = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        return RedirectToPage("/Client/Confirmation", new { codeColis = maj?.CodeColis });
    }

    private Guid? GetClientId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
