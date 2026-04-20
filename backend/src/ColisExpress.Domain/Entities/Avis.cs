namespace ColisExpress.Domain.Entities;

public class Avis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommandeId { get; set; }
    public Guid ClientId { get; set; }
    public Guid TransporteurId { get; set; }
    public int Note { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Commande? Commande { get; set; }
    public Utilisateur? Client { get; set; }
    public Transporteur? Transporteur { get; set; }
}
