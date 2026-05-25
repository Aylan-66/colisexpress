namespace ColisExpress.Application.DTOs.Offres;

public class OffreResponse
{
    public Guid TrajetId { get; init; }
    public Guid TransporteurId { get; init; }
    public string NomTransporteur { get; init; } = string.Empty;
    public string Initiales { get; init; } = "";
    public decimal NoteMoyenne { get; init; }
    public int NombreAvis { get; init; }
    public string VilleDepart { get; init; } = string.Empty;
    public string VilleArrivee { get; init; } = string.Empty;
    public DateTime DateDepart { get; init; }
    public DateTime DateEstimeeArrivee { get; init; }
    public int CapaciteRestante { get; init; }
    public decimal CapaciteMaxPoids { get; init; }
    public string TypeVehicule { get; init; } = string.Empty;
    public decimal Prix { get; init; }
    public decimal PoidsRecherche { get; init; }

    // Relais de départ rattaché au trajet (optionnel)
    public Guid? RelaisDepartId { get; init; }
    public string? RelaisDepartNom { get; init; }
    public string? RelaisDepartAdresse { get; init; }
    public string? RelaisDepartVille { get; init; }
    public double? RelaisDepartLatitude { get; init; }
    public double? RelaisDepartLongitude { get; init; }

    // Tarif paramétrable du trajet (les 3 paliers) — si null, le trajet utilise les anciens prix
    public TarifApercu? Tarif { get; init; }

    // Limites par colis définies sur le trajet (refus auto à la création de commande)
    public int? LongueurMaxColisCm { get; init; }
    public int? LargeurMaxColisCm { get; init; }
    public int? HauteurMaxColisCm { get; init; }
    public decimal? PoidsMaxColisKg { get; init; }

    // Frais de service effectif pour cette offre (override transporteur ou défaut global)
    public string FraisServiceType { get; init; } = "Fixe";   // "Fixe" ou "Pourcentage"
    public decimal FraisServiceValeur { get; init; } = 5m;
}

public class TarifApercu
{
    public string Nom { get; init; } = string.Empty;
    public decimal PrixAuKiloStandard { get; init; }
    public decimal SeuilStandardKg { get; init; }
    public decimal ForfaitLourd { get; init; }
    public decimal PrixAuKiloLourd { get; init; }
    public decimal ForfaitHorsGabarit { get; init; }
    public decimal PrixAuKiloHorsGabarit { get; init; }
    public int LongueurMaxStandardCm { get; init; }
    public int LargeurMaxStandardCm { get; init; }
    public int HauteurMaxStandardCm { get; init; }
}
