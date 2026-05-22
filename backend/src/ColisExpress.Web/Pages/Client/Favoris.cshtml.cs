using System.Security.Claims;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Client;

public class FavorisModel : PageModel
{
    private readonly ColisExpressDbContext _db;
    public FavorisModel(ColisExpressDbContext db) => _db = db;

    public record RelaisFavori(Guid FavoriId, Guid RelaisId, string Nom, string Adresse, string Ville, string Pays);
    public record TransporteurFavori(Guid FavoriId, Guid TransporteurId, string Nom, decimal Note, int NbAvis, string Vehicule);

    public List<RelaisFavori> Relais { get; private set; } = new();
    public List<TransporteurFavori> Transporteurs { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var id = GetUserId();
        if (id is null) return Challenge();
        await LoadAsync(id.Value, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSupprimerAsync(Guid favoriId, CancellationToken ct)
    {
        var id = GetUserId();
        if (id is null) return Challenge();
        var fav = await _db.Favoris.FirstOrDefaultAsync(f => f.Id == favoriId && f.ClientId == id.Value, ct);
        if (fav is not null) { _db.Favoris.Remove(fav); await _db.SaveChangesAsync(ct); }
        return RedirectToPage();
    }

    private async Task LoadAsync(Guid clientId, CancellationToken ct)
    {
        var favs = await _db.Favoris.Where(f => f.ClientId == clientId).ToListAsync(ct);

        var relaisIds = favs.Where(f => f.PointRelaisId != null).Select(f => f.PointRelaisId!.Value).ToList();
        var transpIds = favs.Where(f => f.TransporteurId != null).Select(f => f.TransporteurId!.Value).ToList();

        var relais = await _db.PointsRelais.Where(r => relaisIds.Contains(r.Id)).ToListAsync(ct);
        Relais = favs.Where(f => f.PointRelaisId != null)
            .Select(f =>
            {
                var r = relais.FirstOrDefault(x => x.Id == f.PointRelaisId);
                return r is null ? null : new RelaisFavori(f.Id, r.Id, r.NomRelais, r.Adresse, r.Ville, r.Pays);
            })
            .Where(x => x != null).Cast<RelaisFavori>().ToList();

        var transps = await _db.Transporteurs.Include(t => t.Utilisateur).Where(t => transpIds.Contains(t.Id)).ToListAsync(ct);
        Transporteurs = favs.Where(f => f.TransporteurId != null)
            .Select(f =>
            {
                var t = transps.FirstOrDefault(x => x.Id == f.TransporteurId);
                return t is null ? null : new TransporteurFavori(f.Id, t.Id,
                    t.Utilisateur is null ? "—" : $"{t.Utilisateur.Prenom} {t.Utilisateur.Nom}",
                    t.NoteMoyenne, t.NombreAvis, t.TypeVehicule ?? "—");
            })
            .Where(x => x != null).Cast<TransporteurFavori>().ToList();
    }

    private Guid? GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
