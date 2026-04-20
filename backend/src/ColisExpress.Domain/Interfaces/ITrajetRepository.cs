using ColisExpress.Domain.Entities;

namespace ColisExpress.Domain.Interfaces;

public interface ITrajetRepository
{
    Task<Trajet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Trajet>> GetByTransporteurIdAsync(Guid transporteurId, CancellationToken ct = default);
    Task<IReadOnlyList<Trajet>> SearchAsync(
        string villeDepart,
        string villeArrivee,
        DateTime dateMin,
        decimal poidsKg,
        CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetVillesDepartAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetVillesArriveeAsync(CancellationToken ct = default);
    Task AddAsync(Trajet trajet, CancellationToken ct = default);
    void Update(Trajet trajet);
    void Remove(Trajet trajet);
}
