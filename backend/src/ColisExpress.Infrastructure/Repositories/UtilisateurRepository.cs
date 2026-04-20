using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Repositories;

public class UtilisateurRepository : IUtilisateurRepository
{
    private readonly ColisExpressDbContext _db;

    public UtilisateurRepository(ColisExpressDbContext db) => _db = db;

    public Task<Utilisateur?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Utilisateur?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        _db.Utilisateurs.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(Utilisateur utilisateur, CancellationToken ct = default) =>
        await _db.Utilisateurs.AddAsync(utilisateur, ct);

    public void Update(Utilisateur utilisateur) => _db.Utilisateurs.Update(utilisateur);
}
