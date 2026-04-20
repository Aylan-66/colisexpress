using System.Text;
using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class PaiementsModel : PageModel
{
    private readonly IAdminService _admin;
    public PaiementsModel(IAdminService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)] public string? ModeFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<PaiementAdminListItem> Paiements { get; private set; } = Array.Empty<PaiementAdminListItem>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (Page < 1) Page = 1;
        var (items, totalCount) = await _admin.GetPaiementsAsync(ModeFilter, Page, 20, ct);
        Paiements = items;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / 20.0);
        if (TotalPages < 1) TotalPages = 1;
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken ct)
    {
        var (items, _) = await _admin.GetPaiementsAsync(ModeFilter, 1, 10000, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Code colis;Client;Mode;Montant;Statut;Date");
        foreach (var p in items)
        {
            sb.AppendLine($"{p.CodeColis};{p.Client};{p.Mode};{p.Montant:0.00};{p.Statut};{p.DateCreation:yyyy-MM-dd HH:mm}");
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "paiements.csv");
    }
}
