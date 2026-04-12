using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class SuiviModel : PageModel
{
    private readonly IColisService _colis;

    public SuiviModel(IColisService colis) => _colis = colis;

    [BindProperty(SupportsGet = true)] public string? CodeColis { get; set; }

    public ColisDetailResponse? Colis { get; private set; }
    public bool NotFoundColis { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(CodeColis))
        {
            Colis = await _colis.GetByCodeAsync(CodeColis, ct);
            if (Colis is null) NotFoundColis = true;
        }
    }
}
