using System.Security.Claims;
using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Client;

public class ResultatsModel : PageModel
{
    private readonly IRechercheService _recherche;
    private readonly ColisExpressDbContext _db;

    public ResultatsModel(IRechercheService recherche, ColisExpressDbContext db)
    {
        _recherche = recherche;
        _db = db;
    }

    public HashSet<Guid> FavorisRelais { get; private set; } = new();
    public HashSet<Guid> FavorisTransporteurs { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string VilleDepart { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string VilleArrivee { get; set; } = "";
    [BindProperty(SupportsGet = true, Name = "dateDepart")] public DateTime DateDepart { get; set; }
    [BindProperty(SupportsGet = true)] public decimal Poids { get; set; }
    [BindProperty(SupportsGet = true)] public bool Fragile { get; set; }
    [BindProperty(SupportsGet = true)] public bool Urgent { get; set; }
    [BindProperty(SupportsGet = true)] public bool Assurance { get; set; }
    [BindProperty(SupportsGet = true)] public TriOffres Tri { get; set; } = TriOffres.Prix;

    // Coordonnées géocodées de la ville (passées par l'autocomplétion de la page Recherche)
    [BindProperty(SupportsGet = true, Name = "depLat")] public double? DepLat { get; set; }
    [BindProperty(SupportsGet = true, Name = "depLng")] public double? DepLng { get; set; }

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

        // Charge les favoris du client pour pré-remplir les cœurs
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(s, out var clientId))
        {
            var favs = await _db.Favoris.Where(f => f.ClientId == clientId).ToListAsync(ct);
            FavorisRelais = favs.Where(f => f.PointRelaisId != null).Select(f => f.PointRelaisId!.Value).ToHashSet();
            FavorisTransporteurs = favs.Where(f => f.TransporteurId != null).Select(f => f.TransporteurId!.Value).ToHashSet();
        }
    }
}
