using ColisExpress.Domain.Entities;

namespace ColisExpress.Domain.Interfaces;

public interface IUtilisateurRepository
{
    Task<Utilisateur?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Utilisateur?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(Utilisateur utilisateur, CancellationToken ct = default);
    void Update(Utilisateur utilisateur);
}
