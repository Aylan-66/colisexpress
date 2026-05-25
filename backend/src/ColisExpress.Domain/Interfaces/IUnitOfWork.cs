namespace ColisExpress.Domain.Interfaces;

public interface IUnitOfWork
{
    IUtilisateurRepository Utilisateurs { get; }
    ITransporteurRepository Transporteurs { get; }
    ITrajetRepository Trajets { get; }
    ICommandeRepository Commandes { get; }
    IColisRepository Colis { get; }
    IPaiementRepository Paiements { get; }

    /// Récupère (ou crée) la ligne unique de paramètres plateforme.
    Task<Entities.ParametrePlateforme> GetParametresPlateformeAsync(CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
