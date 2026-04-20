using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Interfaces;

public interface ITransporteurRepository
{
    Task<Transporteur?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Transporteur?> GetByUtilisateurIdAsync(Guid utilisateurId, CancellationToken ct = default);
    Task<IReadOnlyList<Transporteur>> GetByStatutKycAsync(StatutKyc statut, CancellationToken ct = default);
    Task AddAsync(Transporteur transporteur, CancellationToken ct = default);
    void Update(Transporteur transporteur);
}
