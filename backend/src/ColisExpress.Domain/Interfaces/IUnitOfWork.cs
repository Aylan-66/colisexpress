namespace ColisExpress.Domain.Interfaces;

public interface IUnitOfWork
{
    IUtilisateurRepository Utilisateurs { get; }
    ITransporteurRepository Transporteurs { get; }
    ITrajetRepository Trajets { get; }
    ICommandeRepository Commandes { get; }
    IColisRepository Colis { get; }
    IPaiementRepository Paiements { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
