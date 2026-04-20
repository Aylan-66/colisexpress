using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class RechercheModel : PageModel
{
    private readonly IRechercheService _recherche;

    public RechercheModel(IRechercheService recherche) => _recherche = recherche;

    public IReadOnlyList<string> VillesDepart { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> VillesArrivee { get; private set; } = Array.Empty<string>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        var (depart, arrivee) = await _recherche.GetVillesDispoAsync(ct);
        VillesDepart = depart;
        VillesArrivee = arrivee;
    }
}
