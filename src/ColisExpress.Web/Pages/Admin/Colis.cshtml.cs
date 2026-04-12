using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class ColisModel : PageModel
{
    private readonly IAdminService _admin;
    public ColisModel(IAdminService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)] public string? CodeColis { get; set; }
    public ColisDetailResponse? Colis { get; private set; }
    public bool NotFoundColis { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(CodeColis))
        {
            Colis = await _admin.GetColisByCodeAsync(CodeColis, ct);
            if (Colis is null) NotFoundColis = true;
        }
    }
}
