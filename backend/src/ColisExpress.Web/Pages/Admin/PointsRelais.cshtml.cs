using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class PointsRelaisModel : PageModel
{
    private readonly IAdminService _admin;
    public PointsRelaisModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<PointRelaisListItem> PointsRelais { get; private set; } = Array.Empty<PointRelaisListItem>();

    [BindProperty] public CreatePointRelaisRequest NewRelais { get; set; } = new();

    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        PointsRelais = await _admin.GetPointsRelaisAsync(ct);

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        var result = await _admin.CreatePointRelaisAsync(NewRelais, ct);
        if (result.Success)
            Success = "Point relais créé avec succès.";
        else
            Error = result.Error ?? "Erreur lors de la création.";

        PointsRelais = await _admin.GetPointsRelaisAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id, CancellationToken ct)
    {
        var result = await _admin.TogglePointRelaisAsync(id, ct);
        if (result.Success)
            Success = "Statut du point relais mis à jour.";
        else
            Error = result.Error ?? "Erreur lors de la mise à jour.";

        PointsRelais = await _admin.GetPointsRelaisAsync(ct);
        return Page();
    }
}
