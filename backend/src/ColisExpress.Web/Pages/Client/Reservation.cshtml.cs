using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Exceptions;
using ColisExpress.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class ReservationModel : PageModel
{
    private readonly IRechercheService _recherche;
    private readonly ICommandeService _commande;
    private readonly IUnitOfWork _uow;

    public ReservationModel(IRechercheService recherche, ICommandeService commande, IUnitOfWork uow)
    {
        _recherche = recherche;
        _commande = commande;
        _uow = uow;
    }

    [BindProperty(SupportsGet = true)] public Guid TrajetId { get; set; }
    [BindProperty(SupportsGet = true)] public decimal Poids { get; set; }
    [BindProperty(SupportsGet = true)] public bool Fragile { get; set; }
    [BindProperty(SupportsGet = true)] public bool Urgent { get; set; }
    [BindProperty(SupportsGet = true)] public string? SegDepart { get; set; }
    [BindProperty(SupportsGet = true)] public string? SegArrivee { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? RelaisDepartId { get; set; }
    [BindProperty] public IFormFile? PhotoColis { get; set; }

    [BindProperty] public CreateCommandeRequest Input { get; set; } = new();

    public OffreResponse? Offre { get; private set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (TrajetId == Guid.Empty) return RedirectToPage("/Client/Recherche");

        Offre = await _recherche.GetOffreByTrajetIdAsync(TrajetId, Poids <= 0 ? 1 : Poids, ct);
        if (Offre is null) return RedirectToPage("/Client/Recherche");

        Input.TrajetId = TrajetId;
        Input.PoidsDeclare = Poids;
        Input.Fragile = Fragile;
        Input.Urgent = Urgent;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var clientIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Challenge();

        Input.ClientId = clientId;
        Input.TrajetId = TrajetId;
        Input.ModeReglement = ModeReglement.Carte;

        Input.SegmentDepart = SegDepart ?? "";
        Input.SegmentArrivee = SegArrivee ?? "";
        if (RelaisDepartId.HasValue) Input.RelaisDepartId = RelaisDepartId;

        try
        {
            var response = await _commande.CreateAsync(Input, ct);

            // Photo fournie par le client à la réservation (pour la validation du transporteur)
            if (PhotoColis is not null && PhotoColis.Length > 0 && PhotoColis.Length <= 5 * 1024 * 1024)
            {
                var colis = await _uow.Colis.GetByCodeAsync(response.CodeColis, ct);
                if (colis is not null)
                {
                    using var ms = new MemoryStream();
                    await PhotoColis.CopyToAsync(ms, ct);
                    var b64 = Convert.ToBase64String(ms.ToArray());
                    await _uow.Colis.AddEvenementAsync(new EvenementColis
                    {
                        ColisId = colis.Id,
                        AncienStatut = colis.Statut,
                        NouveauStatut = colis.Statut,
                        ActeurId = clientId,
                        Commentaire = "Photo du colis fournie par le client à la réservation",
                        PhotoChemin = $"data:{PhotoColis.ContentType};base64,{b64}"
                    }, ct);
                    await _uow.SaveChangesAsync(ct);
                }
            }

            return RedirectToPage("/Client/Paiement", new { commandeId = response.Id });
        }
        catch (DomainException ex)
        {
            Error = ex.Message;
            Offre = await _recherche.GetOffreByTrajetIdAsync(TrajetId, Poids <= 0 ? 1 : Poids, ct);
            return Page();
        }
    }
}
