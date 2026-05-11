namespace ColisExpress.Domain.Entities;

/// <summary>
/// Tarif paramétrable par transporteur. Trois paliers :
/// - Standard : prix au kilo jusqu'à SeuilStandardKg.
/// - Lourd : forfait + prix au kilo au-dessus de SeuilStandardKg.
/// - Hors gabarit : forfait + prix au kilo si une dimension dépasse les limites définies.
/// </summary>
public class Tarif
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransporteurId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Palier standard
    public decimal PrixAuKiloStandard { get; set; }
    public decimal SeuilStandardKg { get; set; }  // au-dessus → palier lourd

    // Palier lourd
    public decimal ForfaitLourd { get; set; }
    public decimal PrixAuKiloLourd { get; set; }

    // Palier hors gabarit : déclenché si une dimension dépasse les limites
    public decimal ForfaitHorsGabarit { get; set; }
    public decimal PrixAuKiloHorsGabarit { get; set; }
    public int LongueurMaxStandardCm { get; set; }
    public int LargeurMaxStandardCm { get; set; }
    public int HauteurMaxStandardCm { get; set; }

    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Transporteur? Transporteur { get; set; }

    /// <summary>Calcule le prix d'un colis en fonction de son poids et de ses dimensions.</summary>
    public decimal CalculerPrix(decimal poidsKg, int? longueurCm, int? largeurCm, int? hauteurCm)
    {
        var horsGabarit = (longueurCm.HasValue && longueurCm.Value > LongueurMaxStandardCm)
                       || (largeurCm.HasValue && largeurCm.Value > LargeurMaxStandardCm)
                       || (hauteurCm.HasValue && hauteurCm.Value > HauteurMaxStandardCm);

        if (horsGabarit)
            return ForfaitHorsGabarit + PrixAuKiloHorsGabarit * poidsKg;

        if (poidsKg > SeuilStandardKg)
            return ForfaitLourd + PrixAuKiloLourd * poidsKg;

        return PrixAuKiloStandard * poidsKg;
    }
}
