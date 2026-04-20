using ColisExpress.Application.DTOs.Admin;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Pages.Admin;

public class TransporteursModel : PageModel
{
    private readonly IAdminService _admin;
    private readonly ColisExpressDbContext _db;

    public TransporteursModel(IAdminService admin, ColisExpressDbContext db)
    {
        _admin = admin;
        _db = db;
    }

    public IReadOnlyList<TransporteurListItem> Transporteurs { get; private set; } = Array.Empty<TransporteurListItem>();
    [BindProperty(SupportsGet = true)] public Guid? VoirKyc { get; set; }
    public IReadOnlyList<DocumentKycItem> Documents { get; private set; } = Array.Empty<DocumentKycItem>();
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Transporteurs = await _admin.GetTransporteursAsync(ct);
        if (VoirKyc.HasValue)
        {
            Documents = await _db.DocumentsKyc
                .Where(d => d.TransporteurId == VoirKyc.Value)
                .OrderBy(d => d.TypeDocument)
                .Select(d => new DocumentKycItem
                {
                    Id = d.Id,
                    TypeDocument = d.TypeDocument,
                    NomFichier = d.NomFichier,
                    Statut = d.Statut,
                    DateSoumission = d.DateSoumission
                })
                .ToListAsync(ct);
        }
    }

    public async Task<IActionResult> OnPostApproveDocAsync(Guid documentId, Guid voirKyc, CancellationToken ct)
    {
        var result = await _admin.DecideDocumentKycAsync(documentId, true, ct);
        if (result.Success) Success = "Document validé.";
        else Error = result.Error;
        VoirKyc = voirKyc;
        await OnGetAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRejectDocAsync(Guid documentId, Guid voirKyc, CancellationToken ct)
    {
        var result = await _admin.DecideDocumentKycAsync(documentId, false, ct);
        if (result.Success) Success = "Document rejeté. Le transporteur devra le re-soumettre.";
        else Error = result.Error;
        VoirKyc = voirKyc;
        await OnGetAsync(ct);
        return Page();
    }
}
