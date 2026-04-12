using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class Paiement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommandeId { get; set; }
    public ModeReglement Mode { get; set; }
    public decimal Montant { get; set; }
    public StatutReglement Statut { get; set; } = StatutReglement.EnAttente;
    public DateTime? DateEncaissement { get; set; }
    public string? ReferenceExterne { get; set; }
    public Guid? ValidePar { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Commande? Commande { get; set; }
}
