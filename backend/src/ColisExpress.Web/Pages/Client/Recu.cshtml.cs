using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class RecuModel : PageModel
{
    private readonly ColisExpressDbContext _db;
    public RecuModel(ColisExpressDbContext db) => _db = db;

    public Colis? Colis { get; set; }
    public Commande? Commande { get; set; }
    public Trajet? Trajet { get; set; }
    public ColisExpress.Domain.Entities.Transporteur? Transporteur { get; set; }
    public Utilisateur? TransporteurUser { get; set; }
    public PointRelais? RelaisDepart { get; set; }
    public PointRelais? RelaisArrivee { get; set; }
    public DateTime? DateDepotClient { get; set; }

    public async Task<IActionResult> OnGetAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return NotFound();

        Colis = await _db.Colis
            .Include(c => c.Evenements)
            .FirstOrDefaultAsync(c => c.CodeColis == code, ct);
        if (Colis is null) return NotFound();

        Commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == Colis.CommandeId, ct);
        if (Commande is null) return NotFound();

        Trajet = await _db.Trajets.FirstOrDefaultAsync(t => t.Id == Commande.TrajetId, ct);
        if (Trajet is not null)
        {
            Transporteur = await _db.Transporteurs.FirstOrDefaultAsync(t => t.Id == Trajet.TransporteurId, ct);
            if (Transporteur is not null)
                TransporteurUser = await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == Transporteur.UtilisateurId, ct);
        }

        if (Commande.RelaisDepartId.HasValue)
            RelaisDepart = await _db.PointsRelais.FirstOrDefaultAsync(r => r.Id == Commande.RelaisDepartId.Value, ct);
        if (Commande.RelaisArriveeId.HasValue)
            RelaisArrivee = await _db.PointsRelais.FirstOrDefaultAsync(r => r.Id == Commande.RelaisArriveeId.Value, ct);

        DateDepotClient = Colis.Evenements
            .Where(e => e.NouveauStatut == StatutColis.DeposeParClient)
            .OrderBy(e => e.DateHeure)
            .Select(e => (DateTime?)e.DateHeure)
            .FirstOrDefault();

        return Page();
    }
}
