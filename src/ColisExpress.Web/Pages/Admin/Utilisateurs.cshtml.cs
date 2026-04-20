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

    public IReadOnlyList<UtilisateurListItem> Utilisateurs { get; private set; } = Array.Empty<UtilisateurListItem>();

    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        Utilisateurs = await _admin.GetUtilisateursAsync(Search, ct);

    public async Task<IActionResult> OnPostSuspendreAsync(Guid utilisateurId, CancellationToken ct)
    {
        var result = await _admin.SuspendreCompteAsync(utilisateurId, ct);
        if (result.Success)
            Success = "Compte suspendu avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la suspension.";

        Utilisateurs = await _admin.GetUtilisateursAsync(Search, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostReactiverAsync(Guid utilisateurId, CancellationToken ct)
    {
        var result = await _admin.ReactiverCompteAsync(utilisateurId, ct);
        if (result.Success)
            Success = "Compte réactivé avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la réactivation.";

        Utilisateurs = await _admin.GetUtilisateursAsync(Search, ct);
        return Page();
    }
}
