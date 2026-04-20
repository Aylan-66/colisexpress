using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Colis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommandeId { get; set; }
    public string CodeColis { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public string CodeRetrait { get; set; } = string.Empty;
    public decimal? PoidsReel { get; set; }
    public StatutColis Statut { get; set; } = StatutColis.Brouillon;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Commande? Commande { get; set; }
    public ICollection<EvenementColis> Evenements { get; set; } = new List<EvenementColis>();
}
