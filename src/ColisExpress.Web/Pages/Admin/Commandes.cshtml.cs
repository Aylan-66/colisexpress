using System.Text;
using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class CommandesModel : PageModel
{
    private readonly IAdminService _admin;
    public CommandesModel(IAdminService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)] public string? Filtre { get; set; }
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<CommandeAdminListItem> Commandes { get; private set; } = Array.Empty<CommandeAdminListItem>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (Page < 1) Page = 1;
        var (items, totalCount) = await _admin.GetCommandesAsync(Filtre, Page, 20, ct);
        Commandes = items;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / 20.0);
        if (TotalPages < 1) TotalPages = 1;
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken ct)
    {
        var (items, _) = await _admin.GetCommandesAsync(Filtre, 1, 10000, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Code colis;Client;Transporteur;Trajet;Statut colis;Reglement;Total;Date");
        foreach (var c in items)
        {
            sb.AppendLine($"{c.CodeColis};{c.Client};{c.Transporteur};{c.Trajet};{c.StatutColis};{c.StatutReglement};{c.Total:0.00};{c.DateCreation:yyyy-MM-dd HH:mm}");
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "commandes.csv");
    }
}
