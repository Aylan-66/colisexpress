using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Admin;

public class EspecesRelaisModel : PageModel
{
    private readonly ColisExpressDbContext _db;
    public EspecesRelaisModel(ColisExpressDbContext db) => _db = db;

    public record SoldeRelais(Guid RelaisId, string NomRelais, string Ville, decimal SoldeDu, decimal TotalEncaisse, int NbEncaissementsDus, DateTime? DernierEncaissement);
    public record EncaissementItem(Guid PaiementId, Guid CommandeId, string CodeColis, string NomRelais, decimal Montant, DateTime? DateEncaissement, bool EstReverse, DateTime? DateReversement);

    public List<SoldeRelais> Soldes { get; private set; } = new();
    public List<EncaissementItem> Encaissements { get; private set; } = new();
    public decimal TotalDuPlateforme { get; private set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostMarquerReverseAsync(Guid paiementId, CancellationToken ct)
    {
        var p = await _db.Paiements.FirstOrDefaultAsync(x => x.Id == paiementId, ct);
        if (p is null) { Error = "Paiement introuvable."; await LoadAsync(ct); return Page(); }
        if (p.Mode != ModeReglement.Especes) { Error = "Ce paiement n'est pas en espèces."; await LoadAsync(ct); return Page(); }

        p.EstReverseAdmin = true;
        p.DateReversement = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        Success = "Encaissement marqué comme reversé.";
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostMarquerToutReverseAsync(Guid relaisId, CancellationToken ct)
    {
        var paiements = await _db.Paiements
            .Where(p => p.RelaisEncaisseurId == relaisId && p.Mode == ModeReglement.Especes && !p.EstReverseAdmin)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var p in paiements) { p.EstReverseAdmin = true; p.DateReversement = now; }
        await _db.SaveChangesAsync(ct);
        Success = $"{paiements.Count} encaissement(s) marqué(s) comme reversé(s).";
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var paiements = await _db.Paiements
            .Where(p => p.Mode == ModeReglement.Especes && p.RelaisEncaisseurId != null)
            .ToListAsync(ct);

        var relaisIds = paiements.Select(p => p.RelaisEncaisseurId!.Value).Distinct().ToList();
        var relaisDict = await _db.PointsRelais
            .Where(r => relaisIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => (r.NomRelais, r.Ville), ct);

        Soldes = paiements
            .GroupBy(p => p.RelaisEncaisseurId!.Value)
            .Select(g =>
            {
                var info = relaisDict.TryGetValue(g.Key, out var r) ? r : ("(supprimé)", "—");
                return new SoldeRelais(
                    g.Key,
                    info.Item1,
                    info.Item2,
                    g.Where(p => !p.EstReverseAdmin).Sum(p => p.Montant),
                    g.Sum(p => p.Montant),
                    g.Count(p => !p.EstReverseAdmin),
                    g.Max(p => p.DateEncaissement)
                );
            })
            .OrderByDescending(s => s.SoldeDu)
            .ToList();

        TotalDuPlateforme = Soldes.Sum(s => s.SoldeDu);

        // Détail des 100 derniers encaissements
        var commandeIds = paiements.Select(p => p.CommandeId).Distinct().ToList();
        var codesColis = await _db.Colis
            .Where(c => commandeIds.Contains(c.CommandeId))
            .ToDictionaryAsync(c => c.CommandeId, c => c.CodeColis, ct);

        Encaissements = paiements
            .OrderByDescending(p => p.DateEncaissement)
            .Take(100)
            .Select(p => new EncaissementItem(
                p.Id,
                p.CommandeId,
                codesColis.TryGetValue(p.CommandeId, out var code) ? code : "—",
                relaisDict.TryGetValue(p.RelaisEncaisseurId!.Value, out var info) ? info.NomRelais : "—",
                p.Montant,
                p.DateEncaissement,
                p.EstReverseAdmin,
                p.DateReversement
            ))
            .ToList();
    }
}
