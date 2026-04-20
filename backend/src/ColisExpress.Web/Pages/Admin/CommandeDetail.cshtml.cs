using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class CommandeDetailModel : PageModel
{
    private readonly IAdminService _admin;
    public CommandeDetailModel(IAdminService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public CommandeAdminDetail? Commande { get; private set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        Commande = await _admin.GetCommandeDetailAsync(Id, ct);
}
