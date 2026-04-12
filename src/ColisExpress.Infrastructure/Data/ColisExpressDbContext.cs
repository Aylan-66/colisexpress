using System.Reflection;
using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Data;

public class ColisExpressDbContext : DbContext
{
    public ColisExpressDbContext(DbContextOptions<ColisExpressDbContext> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Transporteur> Transporteurs => Set<Transporteur>();
    public DbSet<DocumentKyc> DocumentsKyc => Set<DocumentKyc>();
    public DbSet<PointRelais> PointsRelais => Set<PointRelais>();
    public DbSet<Trajet> Trajets => Set<Trajet>();
    public DbSet<Commande> Commandes => Set<Commande>();
    public DbSet<Colis> Colis => Set<Colis>();
    public DbSet<EvenementColis> EvenementsColis => Set<EvenementColis>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<Avis> Avis => Set<Avis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
