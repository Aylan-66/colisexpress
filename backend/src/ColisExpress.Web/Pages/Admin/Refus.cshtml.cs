using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Admin;

public class RefusModel : PageModel
{
    private readonly ColisExpressDbContext _db;
    public RefusModel(ColisExpressDbContext db) => _db = db;

    public record RefusItem(
        Guid ColisId,
        Guid CommandeId,
        string CodeColis,
        string Client,
        string ClientEmail,
        string Trajet,
        string Segment,
        string MotifRefus,
        DateTime DateRefus,
        string RefuseParRole,
        string? RefuseParNom,
        string ModePaiement,
        decimal Montant,
        string StatutPaiement,
        string? StripeSessionId,
        Guid? RelaisEncaisseurId,
        string? RelaisEncaisseurNom,
        bool EstInspecte,
        DateTime? DateInspection,
        bool RemboursementEffectue
    );

    public List<RefusItem> ANontInspecter { get; private set; } = new();
    public List<RefusItem> Inspectes { get; private set; } = new();
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostMarquerInspecteAsync(Guid colisId, CancellationToken ct)
    {
        var colis = await _db.Colis.FirstOrDefaultAsync(c => c.Id == colisId, ct);
        if (colis is null || colis.Statut != StatutColis.Refuse)
        {
            Error = "Colis introuvable ou non refusé.";
        }
        else
        {
            colis.RefusInspecteAdmin = true;
            colis.RefusInspectionDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            Success = "Refus marqué comme inspecté.";
        }
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostMarquerRembourseAsync(Guid commandeId, CancellationToken ct)
    {
        var commande = await _db.Commandes.FirstOrDefaultAsync(c => c.Id == commandeId, ct);
        if (commande is null)
        {
            Error = "Commande introuvable.";
            await LoadAsync(ct);
            return Page();
        }

        commande.StatutReglement = StatutReglement.Rembourse;

        var paiements = await _db.Paiements
            .Where(p => p.CommandeId == commandeId && p.Statut == StatutReglement.Paye)
            .ToListAsync(ct);
        foreach (var p in paiements)
        {
            p.Statut = StatutReglement.Rembourse;
        }

        await _db.SaveChangesAsync(ct);
        Success = "Commande marquée comme remboursée. Pour Stripe, effectuez le remboursement manuel via dashboard.stripe.com.";
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var refus = await _db.Colis
            .Where(c => c.Statut == StatutColis.Refuse)
            .Select(c => new
            {
                Colis = c,
                Commande = _db.Commandes.FirstOrDefault(co => co.Id == c.CommandeId),
                Trajet = _db.Trajets.FirstOrDefault(t => t.Id == _db.Commandes.Where(co => co.Id == c.CommandeId).Select(co => co.TrajetId).FirstOrDefault()),
                Client = _db.Utilisateurs.FirstOrDefault(u => u.Id == _db.Commandes.Where(co => co.Id == c.CommandeId).Select(co => co.ClientId).FirstOrDefault()),
                Refuseur = c.RefusParUtilisateurId.HasValue
                    ? _db.Utilisateurs.FirstOrDefault(u => u.Id == c.RefusParUtilisateurId.Value)
                    : null,
                Paiement = _db.Paiements.FirstOrDefault(p => p.CommandeId == c.CommandeId && p.Statut != StatutReglement.EnAttente),
                RelaisEncaisseur = _db.Paiements
                    .Where(p => p.CommandeId == c.CommandeId && p.RelaisEncaisseurId != null)
                    .Select(p => p.RelaisEncaisseurId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var relaisIds = refus.Where(r => r.RelaisEncaisseur.HasValue).Select(r => r.RelaisEncaisseur!.Value).Distinct().ToList();
        var relaisDict = await _db.PointsRelais.Where(r => relaisIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.NomRelais, ct);

        var items = refus.Select(r =>
        {
            var c = r.Colis;
            var commande = r.Commande;
            var trajet = r.Trajet;
            var paiement = r.Paiement;
            string? stripeId = paiement?.Mode == ModeReglement.Carte && paiement.ReferenceExterne?.StartsWith("cs_") == true
                ? paiement.ReferenceExterne : null;
            return new RefusItem(
                c.Id,
                commande?.Id ?? Guid.Empty,
                c.CodeColis,
                commande?.Client is null ? (r.Client != null ? $"{r.Client.Prenom} {r.Client.Nom}" : "—") : $"{r.Client?.Prenom} {r.Client?.Nom}",
                r.Client?.Email ?? "—",
                trajet is null ? "—" : $"{trajet.VilleDepart} → {trajet.VilleArrivee}",
                commande is null ? "—" : $"{commande.SegmentDepart} → {commande.SegmentArrivee}",
                c.MotifRefus ?? "—",
                c.DateRefus ?? DateTime.UtcNow,
                c.RefusParRole ?? "—",
                r.Refuseur != null ? $"{r.Refuseur.Prenom} {r.Refuseur.Nom}" : null,
                paiement?.Mode.ToString() ?? "—",
                commande?.Total ?? 0m,
                paiement?.Statut.ToString() ?? "—",
                stripeId,
                r.RelaisEncaisseur,
                r.RelaisEncaisseur.HasValue && relaisDict.TryGetValue(r.RelaisEncaisseur.Value, out var nom) ? nom : null,
                c.RefusInspecteAdmin,
                c.RefusInspectionDate,
                paiement?.Statut == StatutReglement.Rembourse
            );
        }).ToList();

        ANontInspecter = items.Where(i => !i.EstInspecte).OrderByDescending(i => i.DateRefus).ToList();
        Inspectes = items.Where(i => i.EstInspecte).OrderByDescending(i => i.DateInspection).ToList();
    }
}
