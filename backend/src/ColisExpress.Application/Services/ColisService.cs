using ColisExpress.Application.DTOs.Colis;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Interfaces;

namespace ColisExpress.Application.Services;

public class ColisService : IColisService
{
    private readonly IUnitOfWork _uow;

    public ColisService(IUnitOfWork uow) => _uow = uow;

    public async Task<ColisDetailResponse?> GetByCodeAsync(string codeColis, CancellationToken ct = default)
    {
        var colis = await _uow.Colis.GetByCodeWithEvenementsAsync(codeColis.Trim(), ct);
        if (colis is null) return null;

        var commande = colis.Commande;
        var transporteur = commande is null ? null : await _uow.Transporteurs.GetByIdAsync(commande.TransporteurId, ct);
        var utilisateur = transporteur is null ? null : await _uow.Utilisateurs.GetByIdAsync(transporteur.UtilisateurId, ct);
        var nomT = utilisateur is null ? "—" : $"{utilisateur.Prenom} {utilisateur.Nom}";

        return new ColisDetailResponse
        {
            Id = colis.Id,
            CodeColis = colis.CodeColis,
            CodeRetrait = colis.CodeRetrait,
            Statut = colis.Statut,
            PoidsDeclare = commande?.PoidsDeclare ?? 0,
            PoidsReel = colis.PoidsReel,
            Dimensions = commande?.Dimensions,
            VilleDepart = string.IsNullOrEmpty(commande?.SegmentDepart) ? commande?.Trajet?.VilleDepart ?? "—" : commande.SegmentDepart,
            VilleArrivee = string.IsNullOrEmpty(commande?.SegmentArrivee) ? commande?.Trajet?.VilleArrivee ?? "—" : commande.SegmentArrivee,
            NomTransporteur = nomT,
            NomDestinataire = commande?.NomDestinataire ?? "—",
            DateCreation = colis.DateCreation,
            Evenements = colis.Evenements
                .OrderBy(e => e.DateHeure)
                .Select(e => new EvenementColisResponse
                {
                    NouveauStatut = e.NouveauStatut,
                    DateHeure = e.DateHeure,
                    Commentaire = e.Commentaire
                })
                .ToList()
        };
    }
}
