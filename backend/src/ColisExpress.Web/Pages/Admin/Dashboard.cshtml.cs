using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IAdminService _admin;
    public DashboardModel(IAdminService admin) => _admin = admin;

    public DashboardResponse Dashboard { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct) => Dashboard = await _admin.GetDashboardAsync(ct);
}
