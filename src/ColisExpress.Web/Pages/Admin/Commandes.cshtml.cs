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

    public IReadOnlyList<CommandeAdminListItem> Commandes { get; private set; } = Array.Empty<CommandeAdminListItem>();

    public async Task OnGetAsync(CancellationToken ct) =>
        Commandes = await _admin.GetCommandesAsync(Filtre, ct);
}
