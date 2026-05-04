using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IAdminService _admin;
    private readonly ColisExpressDbContext _db;
    public DashboardModel(IAdminService admin, ColisExpressDbContext db)
    {
        _admin = admin;
        _db = db;
    }

    public DashboardResponse Dashboard { get; private set; } = new();
    public int RefusANontInspecter { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Dashboard = await _admin.GetDashboardAsync(ct);
        RefusANontInspecter = await _db.Colis.CountAsync(c => c.Statut == StatutColis.Refuse && !c.RefusInspecteAdmin, ct);
    }
}
