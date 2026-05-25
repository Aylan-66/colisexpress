using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

/// <summary>
/// Paramètres globaux de la plateforme (ligne unique, éditable par l'admin).
/// Pour l'instant : frais de service par défaut.
/// </summary>
public class ParametrePlateforme
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Frais de service par défaut (appliqué si le transporteur n'a pas défini le sien)
    public TypeFraisService FraisServiceType { get; set; } = TypeFraisService.Fixe;
    public decimal FraisServiceValeur { get; set; } = 5m;

    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    /// <summary>Calcule les frais de service pour un prix de transport donné.</summary>
    public decimal CalculerFrais(decimal prixTransport)
        => FraisServiceType == TypeFraisService.Pourcentage
            ? Math.Round(prixTransport * FraisServiceValeur / 100m, 2)
            : FraisServiceValeur;
}
