using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class LitigesModel : PageModel
{
    private readonly IAdminService _admin;
    public LitigesModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<CommandeAdminListItem> ColisEnIncident { get; private set; } = Array.Empty<CommandeAdminListItem>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _admin.GetCommandesAsync(ct);
        ColisEnIncident = all.Where(c =>
            c.StatutColis == StatutColis.Incident ||
            c.StatutColis == StatutColis.Endommage ||
            c.StatutColis == StatutColis.Perdu ||
            c.StatutColis == StatutColis.Refuse ||
            c.StatutColis == StatutColis.RetourExpediteur
        ).ToList();
    }
}
