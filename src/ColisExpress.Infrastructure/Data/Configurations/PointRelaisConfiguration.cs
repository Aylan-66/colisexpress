using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class PointRelaisConfiguration : IEntityTypeConfiguration<PointRelais>
{
    public void Configure(EntityTypeBuilder<PointRelais> builder)
    {
        builder.ToTable("points_relais");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.NomRelais).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Adresse).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Ville).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Pays).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Telephone).HasMaxLength(32).IsRequired();
        builder.Property(p => p.EstActif).IsRequired();

        builder.HasOne(p => p.Utilisateur)
            .WithOne()
            .HasForeignKey<PointRelais>(p => p.UtilisateurId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UtilisateurId).IsUnique();
        builder.HasIndex(p => new { p.Pays, p.Ville });
    }
}
