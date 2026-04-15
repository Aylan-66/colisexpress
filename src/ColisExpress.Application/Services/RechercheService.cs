using ColisExpress.Application.DTOs.Offres;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;

namespace ColisExpress.Application.Services;

public class RechercheService : IRechercheService
{
    private readonly IUnitOfWork _uow;

    public RechercheService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<OffreResponse>> RechercherAsync(RechercheOffreRequest request, CancellationToken ct = default)
    {
        var dateMin = request.DateDepart == default ? DateTime.UtcNow.Date : request.DateDepart.Date;
        dateMin = DateTime.SpecifyKind(dateMin, DateTimeKind.Utc);

        var trajets = await _uow.Trajets.SearchAsync(
            request.VilleDepart,
            request.VilleArrivee,
            dateMin,
            request.Poids,
            ct);

        var offres = trajets.Select(t => ToOffre(t, request.Poids, request.Urgent, request.Fragile));
        offres = request.Tri switch
        {
            TriOffres.Prix => offres.OrderBy(o => o.Prix),
            TriOffres.Note => offres.OrderByDescending(o => o.NoteMoyenne).ThenByDescending(o => o.NombreAvis),
            TriOffres.Rapidite => offres.OrderBy(o => o.DateEstimeeArrivee - o.DateDepart).ThenBy(o => o.DateDepart),
            _ => offres.OrderBy(o => o.Prix)
        };
        return offres.ToList();
    }

    public async Task<OffreResponse?> GetOffreByTrajetIdAsync(Guid trajetId, decimal poids, CancellationToken ct = default)
    {
        var trajet = await _uow.Trajets.GetByIdAsync(trajetId, ct);
        if (trajet is null) return null;
        return ToOffre(trajet, poids, false, false);
    }

    public async Task<(IReadOnlyList<string> Depart, IReadOnlyList<string> Arrivee)> GetVillesDispoAsync(CancellationToken ct = default)
    {
        var depart = await _uow.Trajets.GetVillesDepartAsync(ct);
        var arrivee = await _uow.Trajets.GetVillesArriveeAsync(ct);
        return (depart, arrivee);
    }

    private static OffreResponse ToOffre(Trajet t, decimal poids, bool urgent, bool fragile)
    {
        var transporteur = t.Transporteur;
        var utilisateur = transporteur?.Utilisateur;
        var prenom = utilisateur?.Prenom ?? "";
        var nom = utilisateur?.Nom ?? "";
        var initiales = (prenom.Length > 0 ? prenom[..1] : "") + (nom.Length > 0 ? nom[..1] : "");

        var prix = CalculerPrix(t, poids, urgent, fragile);

        return new OffreResponse
        {
            TrajetId = t.Id,
            TransporteurId = t.TransporteurId,
            NomTransporteur = $"{prenom} {nom}".Trim(),
            Initiales = initiales.ToUpperInvariant(),
            NoteMoyenne = transporteur?.NoteMoyenne ?? 0,
            NombreAvis = transporteur?.NombreAvis ?? 0,
            VilleDepart = t.VilleDepart,
            VilleArrivee = t.VilleArrivee,
            DateDepart = t.DateDepart,
            DateEstimeeArrivee = t.DateEstimeeArrivee,
            CapaciteRestante = t.CapaciteRestante,
            CapaciteMaxPoids = t.CapaciteMaxPoids,
            TypeVehicule = transporteur?.TypeVehicule ?? "Non spécifié",
            Prix = prix,
            PoidsRecherche = poids
        };
    }

    public static decimal CalculerPrix(Trajet t, decimal poids, bool urgent, bool fragile)
    {
        decimal basePrix = t.ModeTarification switch
        {
            ModeTarification.PrixParColis => t.PrixParColis ?? 0,
            ModeTarification.PrixAuKilo => (t.PrixAuKilo ?? 0) * poids,
            ModeTarification.Forfait => (t.PrixParColis ?? 0) + (t.PrixAuKilo ?? 0) * poids,
            _ => 0
        };

        if (urgent) basePrix += t.SupplementUrgent ?? 0;
        if (fragile) basePrix += t.SupplementFragile ?? 0;

        return Math.Round(basePrix, 2);
    }
}
