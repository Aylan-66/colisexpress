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

    // Espèces encaissées par un point relais — suivi du solde dû à la plateforme
    public Guid? RelaisEncaisseurId { get; set; }
    public bool EstReverseAdmin { get; set; } = false;
    public DateTime? DateReversement { get; set; }

    public Commande? Commande { get; set; }
}
