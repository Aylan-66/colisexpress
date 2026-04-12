using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class TransporteursModel : PageModel
{
    private readonly IAdminService _admin;
    public TransporteursModel(IAdminService admin) => _admin = admin;

    public IReadOnlyList<TransporteurListItem> Transporteurs { get; private set; } = Array.Empty<TransporteurListItem>();
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        Transporteurs = await _admin.GetTransporteursAsync(ct);

    public async Task<IActionResult> OnPostApproveAsync(Guid transporteurId, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = transporteurId, Approuver = true }, ct);
        if (result.Success) Success = "KYC approuvé.";
        else Error = result.Error;
        Transporteurs = await _admin.GetTransporteursAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid transporteurId, CancellationToken ct)
    {
        var result = await _admin.DecideKycAsync(new KycDecisionRequest { TransporteurId = transporteurId, Approuver = false }, ct);
        if (result.Success) Success = "KYC rejeté.";
        else Error = result.Error;
        Transporteurs = await _admin.GetTransporteursAsync(ct);
        return Page();
    }
}
