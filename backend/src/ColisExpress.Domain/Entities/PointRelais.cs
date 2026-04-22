using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class PointRelais
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UtilisateurId { get; set; }
    public string NomRelais { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public bool EstActif { get; set; } = true;

    // Horaires d'ouverture
    public string? JoursOuverture { get; set; }       // ex: "Lun,Mar,Mer,Jeu,Ven,Sam"
    public TimeOnly? HeureOuverture { get; set; }      // ex: 08:00
    public TimeOnly? HeureFermeture { get; set; }      // ex: 18:00

    // Commission
    public TypeCommission TypeCommission { get; set; } = TypeCommission.Pourcentage;
    public decimal MontantCommission { get; set; }     // 0 par défaut

    public Utilisateur? Utilisateur { get; set; }

    public bool EstOuvert(DayOfWeek jour, TimeOnly heure)
    {
        if (string.IsNullOrWhiteSpace(JoursOuverture)) return true;
        if (HeureOuverture is null || HeureFermeture is null) return true;

        var joursFr = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "Lun",
            [DayOfWeek.Tuesday] = "Mar",
            [DayOfWeek.Wednesday] = "Mer",
            [DayOfWeek.Thursday] = "Jeu",
            [DayOfWeek.Friday] = "Ven",
            [DayOfWeek.Saturday] = "Sam",
            [DayOfWeek.Sunday] = "Dim"
        };

        if (!JoursOuverture.Contains(joursFr[jour], StringComparison.OrdinalIgnoreCase))
            return false;

        return heure >= HeureOuverture.Value && heure <= HeureFermeture.Value;
    }
}
