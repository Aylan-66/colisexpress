using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class ResultatsModel : PageModel
{
    private readonly IRechercheService _recherche;

    public ResultatsModel(IRechercheService recherche) => _recherche = recherche;

    [BindProperty(SupportsGet = true)] public string VilleDepart { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string VilleArrivee { get; set; } = "";
    [BindProperty(SupportsGet = true, Name = "dateDepart")] public DateTime DateDepart { get; set; }
    [BindProperty(SupportsGet = true)] public decimal Poids { get; set; }
    [BindProperty(SupportsGet = true)] public bool Fragile { get; set; }
    [BindProperty(SupportsGet = true)] public bool Urgent { get; set; }
    [BindProperty(SupportsGet = true)] public bool Assurance { get; set; }
    [BindProperty(SupportsGet = true)] public TriOffres Tri { get; set; } = TriOffres.Prix;

    public IReadOnlyList<OffreResponse> Offres { get; private set; } = Array.Empty<OffreResponse>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(VilleDepart) || string.IsNullOrWhiteSpace(VilleArrivee))
            return;

        if (DateDepart == default) DateDepart = DateTime.UtcNow.Date;

        Offres = await _recherche.RechercherAsync(new RechercheOffreRequest
        {
            VilleDepart = VilleDepart,
            VilleArrivee = VilleArrivee,
            DateDepart = DateDepart,
            Poids = Poids <= 0 ? 1 : Poids,
            Fragile = Fragile,
            Urgent = Urgent,
            Assurance = Assurance,
            Tri = Tri
        }, ct);
    }
}
