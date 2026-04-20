using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class LitigesModel : PageModel
{
    private readonly IAdminService _admin;
    public LitigesModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<CommandeAdminListItem> ColisEnIncident { get; private set; } = Array.Empty<CommandeAdminListItem>();

    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _admin.GetCommandesAsync(null, ct);
        ColisEnIncident = all.Where(c =>
            c.StatutColis == StatutColis.Incident ||
            c.StatutColis == StatutColis.Endommage ||
            c.StatutColis == StatutColis.Perdu ||
            c.StatutColis == StatutColis.Refuse ||
            c.StatutColis == StatutColis.RetourExpediteur
        ).ToList();
    }

    public async Task<IActionResult> OnPostResoudreAsync(Guid commandeId, string commentaire, CancellationToken ct)
    {
        var result = await _admin.ResoudreLitigeAsync(commandeId, commentaire, ct);
        if (result.Success)
            Success = "Litige résolu avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la résolution du litige.";

        var all = await _admin.GetCommandesAsync(null, ct);
        ColisEnIncident = all.Where(c =>
            c.StatutColis == StatutColis.Incident ||
            c.StatutColis == StatutColis.Endommage ||
            c.StatutColis == StatutColis.Perdu ||
            c.StatutColis == StatutColis.Refuse ||
            c.StatutColis == StatutColis.RetourExpediteur
        ).ToList();

        return Page();
    }
}
