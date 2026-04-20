using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class CommandeModel : PageModel
{
    private readonly ICommandeService _commande;
    private readonly IAvisService _avis;

    public CommandeModel(ICommandeService commande, IAvisService avis)
    {
        _commande = commande;
        _avis = avis;
    }

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public CommandeDetailResponse? Commande { get; private set; }
    public AvisResponse? AvisExistant { get; private set; }
    public bool PeutLaisserAvis { get; private set; }
    [BindProperty] public int NoteAvis { get; set; }
    [BindProperty] public string? CommentaireAvis { get; set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();
        await LoadAsync(clientId.Value, ct);
        if (Commande is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAnnulerAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        var result = await _commande.AnnulerAsync(Id, clientId.Value, ct);
        if (result.Success) Success = "Votre commande a été annulée.";
        else Error = result.Error;

        await LoadAsync(clientId.Value, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAvisAsync(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null) return Challenge();

        var result = await _avis.CreateAsync(new CreateAvisRequest
        {
            CommandeId = Id,
            ClientId = clientId.Value,
            Note = NoteAvis,
            Commentaire = CommentaireAvis
        }, ct);

        if (result.Success) Success = "Merci pour votre avis !";
        else Error = result.Error;

        await LoadAsync(clientId.Value, ct);
        return Page();
    }

    private async Task LoadAsync(Guid clientId, CancellationToken ct)
    {
        Commande = await _commande.GetDetailAsync(Id, clientId, ct);
        AvisExistant = await _avis.GetByCommandeIdAsync(Id, ct);
        PeutLaisserAvis = AvisExistant is null && Commande is not null &&
            (Commande.StatutColis == ColisExpress.Domain.Enums.StatutColis.LivraisonCloturee ||
             Commande.StatutColis == ColisExpress.Domain.Enums.StatutColis.RetireParDestinataire ||
             Commande.StatutColis == ColisExpress.Domain.Enums.StatutColis.ReservationConfirmee);
    }

    private Guid? GetClientId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }
}
