using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class CommandeModel : PageModel
{
    private readonly ICommandeService _commande;

    public CommandeModel(ICommandeService commande) => _commande = commande;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public CommandeDetailResponse? Commande { get; private set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();
        Commande = await _commande.GetDetailAsync(Id, clientId.Value, ct);
        if (Commande is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAnnulerAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        var result = await _commande.AnnulerAsync(Id, clientId.Value, ct);
        if (result.Success)
            Success = "Votre commande a été annulée.";
        else
            Error = result.Error;

        Commande = await _commande.GetDetailAsync(Id, clientId.Value, ct);
        return Page();
    }

    private Guid? GetClientId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
