using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class TrajetConfiguration : IEntityTypeConfiguration<Trajet>
{
    public void Configure(EntityTypeBuilder<Trajet> builder)
    {
        builder.ToTable("trajets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.PaysDepart).HasMaxLength(100).IsRequired();
        builder.Property(t => t.VilleDepart).HasMaxLength(150).IsRequired();
        builder.Property(t => t.PaysArrivee).HasMaxLength(100).IsRequired();
        builder.Property(t => t.VilleArrivee).HasMaxLength(150).IsRequired();
        builder.Property(t => t.DateDepart).IsRequired();
        builder.Property(t => t.DateEstimeeArrivee).IsRequired();
        builder.Property(t => t.CapaciteMaxPoids).HasPrecision(10, 2).IsRequired();
        builder.Property(t => t.NombreMaxColis).IsRequired();
        builder.Property(t => t.CapaciteRestante).IsRequired();
        builder.Property(t => t.ModeTarification).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.PrixParColis).HasPrecision(10, 2);
        builder.Property(t => t.PrixAuKilo).HasPrecision(10, 2);
        builder.Property(t => t.SupplementUrgent).HasPrecision(10, 2);
        builder.Property(t => t.SupplementFragile).HasPrecision(10, 2);
        builder.Property(t => t.PointDepot).HasMaxLength(500);
        builder.Property(t => t.Conditions).HasMaxLength(2000);
        builder.Property(t => t.Statut).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.DateCreation).IsRequired();

        builder.HasOne(t => t.Transporteur)
            .WithMany()
            .HasForeignKey(t => t.TransporteurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TransporteurId);
        builder.HasIndex(t => new { t.VilleDepart, t.VilleArrivee, t.DateDepart });
        builder.HasIndex(t => t.Statut);
    }
}
