using System.Text;
using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class UtilisateursModel : PageModel
{
    private readonly IAdminService _admin;
    public UtilisateursModel(IAdminService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<UtilisateurListItem> Utilisateurs { get; private set; } = Array.Empty<UtilisateurListItem>();

    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (Page < 1) Page = 1;
        var (items, totalCount) = await _admin.GetUtilisateursAsync(Search, Page, 20, ct);
        Utilisateurs = items;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / 20.0);
        if (TotalPages < 1) TotalPages = 1;
    }

    public async Task<IActionResult> OnGetExportCsvAsync(CancellationToken ct)
    {
        var (items, _) = await _admin.GetUtilisateursAsync(Search, 1, 10000, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Nom;Prenom;Email;Role;Statut;Date inscription");
        foreach (var u in items)
        {
            sb.AppendLine($"{u.Nom};{u.Prenom};{u.Email};{u.Role};{u.StatutCompte};{u.DateCreation:yyyy-MM-dd}");
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "utilisateurs.csv");
    }

    public async Task<IActionResult> OnPostSuspendreAsync(Guid utilisateurId, CancellationToken ct)
    {
        var result = await _admin.SuspendreCompteAsync(utilisateurId, ct);
        if (result.Success)
            Success = "Compte suspendu avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la suspension.";

        var (items, totalCount) = await _admin.GetUtilisateursAsync(Search, Page, 20, ct);
        Utilisateurs = items;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / 20.0);
        if (TotalPages < 1) TotalPages = 1;
        return Page();
    }

    public async Task<IActionResult> OnPostReactiverAsync(Guid utilisateurId, CancellationToken ct)
    {
        var result = await _admin.ReactiverCompteAsync(utilisateurId, ct);
        if (result.Success)
            Success = "Compte réactivé avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la réactivation.";

        var (items, totalCount) = await _admin.GetUtilisateursAsync(Search, Page, 20, ct);
        Utilisateurs = items;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / 20.0);
        if (TotalPages < 1) TotalPages = 1;
        return Page();
    }
}
