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

    public async Task OnGetAsync(CancellationToken ct) =>
        Utilisateurs = await _admin.GetUtilisateursAsync(Search, ct);
}
