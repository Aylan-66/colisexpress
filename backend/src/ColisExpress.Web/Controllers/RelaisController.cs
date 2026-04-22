using System.Security.Claims;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Web.Controllers;

[Route("api/relais")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RelaisController : ControllerBase
{
    private readonly ColisExpressDbContext _db;
    private readonly IUnitOfWork _uow;

    public RelaisController(ColisExpressDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    [HttpGet("profil")]
    public async Task<IActionResult> GetProfil(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        return Ok(new
        {
            id = relais.Id,
            nomRelais = relais.NomRelais,
            adresse = relais.Adresse,
            ville = relais.Ville,
            pays = relais.Pays,
            telephone = relais.Telephone,
            estActif = relais.EstActif,
            joursOuverture = relais.JoursOuverture ?? "",
            heureOuverture = relais.HeureOuverture?.ToString("HH:mm"),
            heureFermeture = relais.HeureFermeture?.ToString("HH:mm"),
            heureOuvertureWeekend = relais.HeureOuvertureWeekend?.ToString("HH:mm"),
            heureFermetureWeekend = relais.HeureFermetureWeekend?.ToString("HH:mm"),
            typeCommission = relais.TypeCommission.ToString(),
            montantCommission = relais.MontantCommission
        });
    }

    [HttpPut("profil")]
    public async Task<IActionResult> UpdateProfil([FromBody] UpdateRelaisProfilRequest request, CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        if (!string.IsNullOrWhiteSpace(request.NomRelais)) relais.NomRelais = request.NomRelais.Trim();
        if (!string.IsNullOrWhiteSpace(request.Adresse)) relais.Adresse = request.Adresse.Trim();
        if (!string.IsNullOrWhiteSpace(request.Ville)) relais.Ville = request.Ville.Trim();
        if (!string.IsNullOrWhiteSpace(request.Pays)) relais.Pays = request.Pays.Trim();
        if (!string.IsNullOrWhiteSpace(request.Telephone)) relais.Telephone = request.Telephone.Trim();

        if (request.JoursOuverture is not null) relais.JoursOuverture = request.JoursOuverture;
        if (request.HeureOuverture is not null && TimeOnly.TryParse(request.HeureOuverture, out var ho))
            relais.HeureOuverture = ho;
        if (request.HeureFermeture is not null && TimeOnly.TryParse(request.HeureFermeture, out var hf))
            relais.HeureFermeture = hf;
        if (request.HeureOuvertureWeekend is not null && TimeOnly.TryParse(request.HeureOuvertureWeekend, out var how))
            relais.HeureOuvertureWeekend = how;
        if (request.HeureFermetureWeekend is not null && TimeOnly.TryParse(request.HeureFermetureWeekend, out var hfw))
            relais.HeureFermetureWeekend = hfw;

        if (request.TypeCommission is not null && Enum.TryParse<TypeCommission>(request.TypeCommission, true, out var tc))
            relais.TypeCommission = tc;
        if (request.MontantCommission.HasValue)
            relais.MontantCommission = request.MontantCommission.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Profil mis à jour." });
    }

    [HttpGet("colis")]
    public async Task<IActionResult> GetColis(CancellationToken ct)
    {
        var relais = await GetRelaisAsync(ct);
        if (relais is null) return NotFound(new { error = "Profil point relais introuvable." });

        var commandes = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Trajet)
            .Include(c => c.Client)
            .Where(c => c.RelaisArriveeId == relais.Id || c.RelaisDepartId == relais.Id)
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

        // Aussi inclure les colis en statut "ArriveDansPaysDest" sur des trajets allant vers la ville du relais
        var colisEnRoute = await _db.Commandes
            .Include(c => c.Colis)
            .Include(c => c.Trajet)
            .Include(c => c.Client)
            .Where(c => c.Colis != null &&
                (c.Colis.Statut == StatutColis.ArriveDansPaysDest ||
                 c.Colis.Statut == StatutColis.ReceptionneParPointRelais ||
                 c.Colis.Statut == StatutColis.DisponibleAuRetrait) &&
                c.VilleDestinataire.ToLower() == relais.Ville.ToLower())
            .OrderByDescending(c => c.DateCreation)
            .ToListAsync(ct);

        var allCommandes = commandes.Union(colisEnRoute).DistinctBy(c => c.Id).ToList();

        return Ok(allCommandes.Where(c => c.Colis is not null).Select(c => new
        {
            commandeId = c.Id,
            codeColis = c.Colis!.CodeColis,
            statut = c.Colis.Statut.ToString(),
            nomDestinataire = c.NomDestinataire,
            telephoneDestinataire = c.TelephoneDestinataire,
            villeDestinataire = c.VilleDestinataire,
            codeRetrait = c.Colis.CodeRetrait,
            poidsDeclare = c.PoidsDeclare,
            total = c.Total,
            trajet = c.Trajet is null ? "—" : $"{c.Trajet.VilleDepart} → {c.Trajet.VilleArrivee}",
            client = c.Client is null ? "—" : $"{c.Client.Prenom} {c.Client.Nom}",
            dateCreation = c.DateCreation
        }));
    }

    [HttpPost("colis/{codeColis}/confirmer-depot")]
    public async Task<IActionResult> ConfirmerDepot(string codeColis, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        var ancien = colis.Statut;
        colis.Statut = StatutColis.ReceptionneParPointRelais;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = ancien,
            NouveauStatut = StatutColis.ReceptionneParPointRelais,
            ActeurId = GetUserId(),
            Commentaire = "Colis réceptionné par le point relais"
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = colis.Statut.ToString(), message = "Dépôt confirmé." });
    }

    [HttpPost("colis/{codeColis}/confirmer-retrait")]
    public async Task<IActionResult> ConfirmerRetrait(string codeColis, [FromBody] ConfirmerRetraitRequest request, CancellationToken ct)
    {
        var colis = await _uow.Colis.GetByCodeAsync(codeColis, ct);
        if (colis is null) return NotFound(new { error = "Colis introuvable." });

        if (colis.CodeRetrait != request.CodeRetrait)
            return BadRequest(new { error = "Code de retrait incorrect." });

        var ancien = colis.Statut;
        colis.Statut = StatutColis.RetireParDestinataire;

        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = ancien,
            NouveauStatut = StatutColis.RetireParDestinataire,
            ActeurId = GetUserId(),
            Commentaire = "Colis retiré par le destinataire (code vérifié)"
        }, ct);

        colis.Statut = StatutColis.LivraisonCloturee;
        await _uow.Colis.AddEvenementAsync(new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = StatutColis.RetireParDestinataire,
            NouveauStatut = StatutColis.LivraisonCloturee,
            ActeurId = GetUserId(),
            Commentaire = "Livraison clôturée"
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Ok(new { statut = colis.Statut.ToString(), message = "Retrait confirmé, livraison clôturée." });
    }

    [HttpPost("paiement/{commandeId:guid}/valider-especes")]
    public async Task<IActionResult> ValiderPaiementEspeces(Guid commandeId, CancellationToken ct)
    {
        var commande = await _db.Commandes
            .Include(c => c.Colis)
            .FirstOrDefaultAsync(c => c.Id == commandeId, ct);

        if (commande is null) return NotFound(new { error = "Commande introuvable." });

        if (commande.StatutReglement == StatutReglement.Paye)
            return Ok(new { message = "Déjà payé." });

        commande.StatutReglement = StatutReglement.Paye;

        var paiement = new Paiement
        {
            CommandeId = commande.Id,
            Mode = ModeReglement.Especes,
            Montant = commande.Total,
            Statut = StatutReglement.Paye,
            DateEncaissement = DateTime.UtcNow,
            ReferenceExterne = $"Espèces validé par relais {GetUserId()}"
        };
        await _db.Paiements.AddAsync(paiement, ct);

        if (commande.Colis is not null && commande.Colis.Statut == StatutColis.DemandeCreee)
        {
            commande.Colis.Statut = StatutColis.ReservationConfirmee;
            await _uow.Colis.AddEvenementAsync(new EvenementColis
            {
                ColisId = commande.Colis.Id,
                AncienStatut = StatutColis.DemandeCreee,
                NouveauStatut = StatutColis.ReservationConfirmee,
                ActeurId = GetUserId(),
                Commentaire = "Paiement espèces validé par le point relais"
            }, ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Paiement espèces validé." });
    }

    private async Task<PointRelais?> GetRelaisAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        return await _db.PointsRelais.FirstOrDefaultAsync(p => p.UtilisateurId == userId, ct);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class UpdateRelaisProfilRequest
{
    public string? NomRelais { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? Telephone { get; set; }
    public string? JoursOuverture { get; set; }
    public string? HeureOuverture { get; set; }
    public string? HeureFermeture { get; set; }
    public string? HeureOuvertureWeekend { get; set; }
    public string? HeureFermetureWeekend { get; set; }
    public string? TypeCommission { get; set; }
    public decimal? MontantCommission { get; set; }
}

public class ConfirmerRetraitRequest
{
    public string CodeRetrait { get; set; } = string.Empty;
}
