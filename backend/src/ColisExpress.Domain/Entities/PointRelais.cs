using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class PointRelais
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UtilisateurId { get; set; }
    public string NomRelais { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Departement { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public bool EstActif { get; set; } = true;

    // Coordonnées géographiques (carte côté client)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Horaires d'ouverture semaine
    public string? JoursOuverture { get; set; }       // ex: "Lun,Mar,Mer,Jeu,Ven,Sam"
    public TimeOnly? HeureOuverture { get; set; }      // ex: 08:00
    public TimeOnly? HeureFermeture { get; set; }      // ex: 18:00

    // Horaires weekend (Sam/Dim si différents)
    public TimeOnly? HeureOuvertureWeekend { get; set; }  // ex: 09:00
    public TimeOnly? HeureFermetureWeekend { get; set; }  // ex: 14:00

    // Commission
    public TypeCommission TypeCommission { get; set; } = TypeCommission.Pourcentage;
    public decimal MontantCommission { get; set; }     // 0 par défaut

    public Utilisateur? Utilisateur { get; set; }

    public bool EstOuvert(DayOfWeek jour, TimeOnly heure)
    {
        if (string.IsNullOrWhiteSpace(JoursOuverture)) return true;

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

        var isWeekend = jour == DayOfWeek.Saturday || jour == DayOfWeek.Sunday;

        TimeOnly? ouv, fer;
        if (isWeekend && HeureOuvertureWeekend.HasValue)
        {
            ouv = HeureOuvertureWeekend;
            fer = HeureFermetureWeekend;
        }
        else
        {
            ouv = HeureOuverture;
            fer = HeureFermeture;
        }

        if (ouv is null || fer is null) return true;
        return heure >= ouv.Value && heure <= fer.Value;
    }
}
