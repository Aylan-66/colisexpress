namespace ColisExpress.Domain;

public static class RulesMetier
{
    public static class ProduitsInterdits
    {
        public static readonly IReadOnlyList<string> MotsCles = new[]
        {
            "arme", "armes", "munition", "munitions",
            "explosif", "explosifs", "feu d'artifice", "feux d'artifice",
            "drogue", "drogues", "stupefiant", "stupéfiant", "stupefiants", "stupéfiants",
            "liquide inflammable", "essence", "alcool pur", "ethanol", "éthanol",
            "batterie lithium", "batteries lithium", "gaz sous pression",
            "animaux vivants", "animal vivant",
            "argent liquide", "billets de banque",
            "bijoux en or", "lingot", "lingots",
            "drone militaire", "matières radioactives", "matieres radioactives"
        };

        public static (bool Interdit, string? MotTrouve) Check(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return (false, null);
            var lower = description.ToLowerInvariant();
            foreach (var mot in MotsCles)
            {
                if (lower.Contains(mot)) return (true, mot);
            }
            return (false, null);
        }
    }

    public static class Cotransportage
    {
        public const decimal PlafondAnnuelEuros = 5000m;
        public const decimal SeuilAlerte = 4000m;
    }

    public static class Annulation
    {
        public static bool EstAnnulable(Domain.Enums.StatutColis statut) => statut switch
        {
            Domain.Enums.StatutColis.Brouillon => true,
            Domain.Enums.StatutColis.DemandeCreee => true,
            Domain.Enums.StatutColis.EnAttenteReglement => true,
            Domain.Enums.StatutColis.ReservationConfirmee => true,
            Domain.Enums.StatutColis.CodeColisGenere => true,
            Domain.Enums.StatutColis.EnAttenteDepot => true,
            _ => false
        };
    }
}
