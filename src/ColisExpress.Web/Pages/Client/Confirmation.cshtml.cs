using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class ConfirmationModel : PageModel
{
    private readonly IColisService _colis;
    private readonly IQrCodeService _qr;

    public ConfirmationModel(IColisService colis, IQrCodeService qr)
    {
        _colis = colis;
        _qr = qr;
    }

    [BindProperty(SupportsGet = true)] public string CodeColis { get; set; } = "";

    public ColisDetailResponse? Colis { get; private set; }
    public string? QrCodeBase64 { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(CodeColis)) return NotFound();
        Colis = await _colis.GetByCodeAsync(CodeColis, ct);
        if (Colis is null) return NotFound();
        QrCodeBase64 = _qr.GenerateBase64Png(Colis.CodeColis);
        return Page();
    }
}
