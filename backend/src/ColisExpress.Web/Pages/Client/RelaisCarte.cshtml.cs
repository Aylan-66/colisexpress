using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class RelaisCarteModel : PageModel
{
    private readonly ColisExpressDbContext _db;
    public RelaisCarteModel(ColisExpressDbContext db) => _db = db;

    public record RelaisCarte(Guid Id, string Nom, string Adresse, string Ville, string Pays, string Telephone, double Lat, double Lng, string? Horaires);

    public List<RelaisCarte> Relais { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Relais = await _db.PointsRelais
            .Where(r => r.EstActif && r.Latitude != null && r.Longitude != null)
            .Select(r => new RelaisCarte(
                r.Id,
                r.NomRelais,
                r.Adresse,
                r.Ville,
                r.Pays,
                r.Telephone,
                r.Latitude!.Value,
                r.Longitude!.Value,
                r.JoursOuverture != null && r.HeureOuverture != null && r.HeureFermeture != null
                    ? $"{r.JoursOuverture} {r.HeureOuverture:HH\\:mm}-{r.HeureFermeture:HH\\:mm}"
                    : null
            ))
            .ToListAsync(ct);
    }
}
