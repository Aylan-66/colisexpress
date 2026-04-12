using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class PaiementModel : PageModel
{
    private readonly ICommandeService _commande;

    public PaiementModel(ICommandeService commande) => _commande = commande;

    [BindProperty(SupportsGet = true)] public Guid CommandeId { get; set; }

    public CommandeResponse? Commande { get; private set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        if (Commande is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        try
        {
            await _commande.ConfirmerPaiementAsync(CommandeId, clientId.Value, ct);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            Commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
            return Page();
        }

        var commande = await _commande.GetByIdAsync(CommandeId, clientId.Value, ct);
        return RedirectToPage("/Client/Confirmation", new { codeColis = commande?.CodeColis });
    }

    private Guid? GetClientId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
