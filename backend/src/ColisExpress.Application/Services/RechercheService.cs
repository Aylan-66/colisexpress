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

        var param = await _uow.GetParametresPlateformeAsync(ct);
        var offres = trajets.Select(t => ToOffre(t, request.Poids, request.Urgent, request.Fragile, request.VilleDepart, request.VilleArrivee, param.FraisServiceType.ToString(), param.FraisServiceValeur));
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
        var param = await _uow.GetParametresPlateformeAsync(ct);
        return ToOffre(trajet, poids, false, false, defautFraisType: param.FraisServiceType.ToString(), defautFraisValeur: param.FraisServiceValeur);
    }

    public async Task<(IReadOnlyList<string> Depart, IReadOnlyList<string> Arrivee)> GetVillesDispoAsync(CancellationToken ct = default)
    {
        var depart = await _uow.Trajets.GetVillesDepartAsync(ct);
        var arrivee = await _uow.Trajets.GetVillesArriveeAsync(ct);
        return (depart, arrivee);
    }

    private static OffreResponse ToOffre(Trajet t, decimal poids, bool urgent, bool fragile,
        string? rechercheDepart = null, string? rechercheArrivee = null,
        string defautFraisType = "Fixe", decimal defautFraisValeur = 5m)
    {
        var transporteur = t.Transporteur;
        var utilisateur = transporteur?.Utilisateur;
        var prenom = utilisateur?.Prenom ?? "";
        var nom = utilisateur?.Nom ?? "";
        var initiales = (prenom.Length > 0 ? prenom[..1] : "") + (nom.Length > 0 ? nom[..1] : "");

        var prix = CalculerPrix(t, poids, urgent, fragile);

        // Dates : utiliser les étapes si le match est via étapes intermédiaires
        var dateDepart = t.DateDepart;
        var dateArrivee = t.DateEstimeeArrivee;
        var villeDepart = t.VilleDepart;
        var villeArrivee = t.VilleArrivee;

        // Relais de départ : par défaut celui du trajet, sinon l'étape qui correspond à la recherche
        PointRelais? relaisDepart = t.RelaisDepart;

        if (rechercheDepart is not null && rechercheArrivee is not null && t.Etapes?.Count > 0)
        {
            var dep = rechercheDepart.ToLowerInvariant();
            var arr = rechercheArrivee.ToLowerInvariant();
            var etapesOrdonnees = t.Etapes.OrderBy(e => e.Ordre).ToList();

            // Trouver l'étape de départ (relais officiel OU point perso)
            if (t.VilleDepart.ToLowerInvariant() != dep)
            {
                var etapeDep = etapesOrdonnees.FirstOrDefault(e =>
                    e.PointRelais?.Ville.ToLowerInvariant() == dep
                    || e.PointTransporteur?.Ville.ToLowerInvariant() == dep);
                if (etapeDep is not null)
                {
                    dateDepart = etapeDep.HeureEstimeeArrivee;
                    villeDepart = etapeDep.PointRelais?.Ville ?? etapeDep.PointTransporteur?.Ville ?? villeDepart;
                    relaisDepart = etapeDep.PointRelais;
                }
            }

            // Trouver l'étape d'arrivée
            if (t.VilleArrivee.ToLowerInvariant() != arr)
            {
                var etapeArr = etapesOrdonnees.FirstOrDefault(e =>
                    e.PointRelais?.Ville.ToLowerInvariant() == arr
                    || e.PointTransporteur?.Ville.ToLowerInvariant() == arr);
                if (etapeArr is not null)
                {
                    dateArrivee = etapeArr.HeureEstimeeArrivee;
                    villeArrivee = etapeArr.PointRelais?.Ville ?? etapeArr.PointTransporteur?.Ville ?? villeArrivee;
                }
            }
        }

        // Si pas de relais officiel en départ, fallback sur le 1er point perso présent dans les étapes
        // (cas trajet créé avec un point perso comme étape de départ)
        PointTransporteur? pointPerso = null;
        if (relaisDepart is null && t.Etapes != null)
        {
            var premiereEtape = t.Etapes.OrderBy(e => e.Ordre).FirstOrDefault();
            if (premiereEtape?.PointTransporteur != null)
                pointPerso = premiereEtape.PointTransporteur;
        }

        var typePoint = relaisDepart != null ? "officiel" : (pointPerso != null ? "perso" : "trajet");
        var pointId = relaisDepart?.Id ?? pointPerso?.Id;
        var pointNom = relaisDepart?.NomRelais ?? pointPerso?.Nom;
        var pointAdresse = relaisDepart?.Adresse ?? pointPerso?.Adresse;
        var pointVille = relaisDepart?.Ville ?? pointPerso?.Ville;
        var pointLat = relaisDepart?.Latitude ?? pointPerso?.Latitude;
        var pointLng = relaisDepart?.Longitude ?? pointPerso?.Longitude;

        return new OffreResponse
        {
            TrajetId = t.Id,
            TransporteurId = t.TransporteurId,
            NomTransporteur = $"{prenom} {nom}".Trim(),
            Initiales = initiales.ToUpperInvariant(),
            NoteMoyenne = transporteur?.NoteMoyenne ?? 0,
            NombreAvis = transporteur?.NombreAvis ?? 0,
            VilleDepart = villeDepart,
            VilleArrivee = villeArrivee,
            DateDepart = dateDepart,
            DateEstimeeArrivee = dateArrivee,
            CapaciteRestante = t.CapaciteRestante,
            CapaciteMaxPoids = t.CapaciteMaxPoids,
            TypeVehicule = transporteur?.TypeVehicule ?? "Non spécifié",
            Prix = prix,
            PoidsRecherche = poids,
            RelaisDepartId = pointId,
            RelaisDepartNom = pointNom,
            RelaisDepartAdresse = pointAdresse,
            RelaisDepartVille = pointVille,
            RelaisDepartLatitude = pointLat,
            RelaisDepartLongitude = pointLng,
            TypePointDepart = typePoint,
            LongueurMaxColisCm = t.LongueurMaxColisCm,
            LargeurMaxColisCm = t.LargeurMaxColisCm,
            HauteurMaxColisCm = t.HauteurMaxColisCm,
            PoidsMaxColisKg = t.PoidsMaxColisKg,
            // Frais effectif : override transporteur si défini, sinon défaut global
            FraisServiceType = transporteur?.FraisServiceType?.ToString() ?? defautFraisType,
            FraisServiceValeur = transporteur?.FraisServiceValeur ?? defautFraisValeur,
            Tarif = t.Tarif is null ? null : new TarifApercu
            {
                Nom = t.Tarif.Nom,
                PrixAuKiloStandard = t.Tarif.PrixAuKiloStandard,
                SeuilStandardKg = t.Tarif.SeuilStandardKg,
                ForfaitLourd = t.Tarif.ForfaitLourd,
                PrixAuKiloLourd = t.Tarif.PrixAuKiloLourd,
                ForfaitHorsGabarit = t.Tarif.ForfaitHorsGabarit,
                PrixAuKiloHorsGabarit = t.Tarif.PrixAuKiloHorsGabarit,
                LongueurMaxStandardCm = t.Tarif.LongueurMaxStandardCm,
                LargeurMaxStandardCm = t.Tarif.LargeurMaxStandardCm,
                HauteurMaxStandardCm = t.Tarif.HauteurMaxStandardCm,
            }
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
