using System.Security.Claims;
using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class ReservationModel : PageModel
{
    private readonly IRechercheService _recherche;
    private readonly ICommandeService _commande;

    public ReservationModel(IRechercheService recherche, ICommandeService commande)
    {
        _recherche = recherche;
        _commande = commande;
    }

    [BindProperty(SupportsGet = true)] public Guid TrajetId { get; set; }
    [BindProperty(SupportsGet = true)] public decimal Poids { get; set; }
    [BindProperty(SupportsGet = true)] public bool Fragile { get; set; }
    [BindProperty(SupportsGet = true)] public bool Urgent { get; set; }
    [BindProperty(SupportsGet = true)] public string? SegDepart { get; set; }
    [BindProperty(SupportsGet = true)] public string? SegArrivee { get; set; }

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

        try
        {
            var response = await _commande.CreateAsync(Input, ct);
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
