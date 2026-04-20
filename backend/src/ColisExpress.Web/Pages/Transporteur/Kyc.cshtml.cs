using System.Security.Claims;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Transporteur;

public class KycModel : PageModel
{
    private readonly ITransporteurService _transporteur;

    public KycModel(ITransporteurService transporteur) => _transporteur = transporteur;

    public TransporteurDashboardResponse? Dashboard { get; private set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public static readonly (TypeDocument Type, string Label, string Hint)[] TypesDocuments =
    {
        (TypeDocument.PieceIdentite, "Pièce d'identité", "Carte nationale d'identité ou passeport"),
        (TypeDocument.JustificatifAdresse, "Justificatif de domicile", "Facture de moins de 3 mois"),
        (TypeDocument.Assurance, "Attestation d'assurance", "Assurance responsabilité civile / transport"),
        (TypeDocument.Permis, "Permis de conduire", "Si applicable à votre véhicule"),
        (TypeDocument.Selfie, "Selfie avec pièce d'identité", "Prenez-vous en photo avec votre carte d'identité")
    };

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Challenge();
        Dashboard = await _transporteur.GetDashboardAsync(userId.Value, ct);
        if (Dashboard is null) return RedirectToPage("/Client/Profil");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile fichier, TypeDocument typeDocument, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Challenge();

        Dashboard = await _transporteur.GetDashboardAsync(userId.Value, ct);
        if (Dashboard is null) return RedirectToPage("/Client/Profil");

        if (fichier is null || fichier.Length == 0)
        {
            Error = "Veuillez sélectionner un fichier.";
            return Page();
        }

        using var ms = new MemoryStream();
        await fichier.CopyToAsync(ms, ct);

        var result = await _transporteur.UploadDocumentAsync(new UploadDocumentKycRequest
        {
            TransporteurId = Dashboard.TransporteurId,
            TypeDocument = typeDocument,
            NomFichier = fichier.FileName,
            ContentType = fichier.ContentType,
            Contenu = ms.ToArray()
        }, ct);

        if (result.Success)
        {
            Success = $"Document « {typeDocument} » soumis avec succès.";
            Dashboard = await _transporteur.GetDashboardAsync(userId.Value, ct);
        }
        else
        {
            Error = result.Error;
        }

        return Page();
    }

    private Guid? GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
