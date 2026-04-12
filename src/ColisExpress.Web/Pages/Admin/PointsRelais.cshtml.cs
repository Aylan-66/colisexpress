using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class PointsRelaisModel : PageModel
{
    private readonly IAdminService _admin;
    public PointsRelaisModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<PointRelaisListItem> PointsRelais { get; private set; } = Array.Empty<PointRelaisListItem>();

    public async Task OnGetAsync(CancellationToken ct) =>
        PointsRelais = await _admin.GetPointsRelaisAsync(ct);
}
