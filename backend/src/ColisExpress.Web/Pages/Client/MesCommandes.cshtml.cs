using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class MesCommandesModel : PageModel
{
    private readonly ICommandeService _commande;

    public MesCommandesModel(ICommandeService commande) => _commande = commande;

    [BindProperty(SupportsGet = true)] public FiltreCommandes Filtre { get; set; } = FiltreCommandes.Toutes;

    public IReadOnlyList<CommandeListItem> Commandes { get; private set; } = Array.Empty<CommandeListItem>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idStr, out var id))
        {
            Commandes = await _commande.GetCommandesClientAsync(id, Filtre, ct);
        }
    }
}
