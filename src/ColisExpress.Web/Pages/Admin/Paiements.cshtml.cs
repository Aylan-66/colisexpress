using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class PaiementsModel : PageModel
{
    private readonly IAdminService _admin;
    public PaiementsModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<PaiementAdminListItem> Paiements { get; private set; } = Array.Empty<PaiementAdminListItem>();

    public async Task OnGetAsync(CancellationToken ct) =>
        Paiements = await _admin.GetPaiementsAsync(ct);
}
