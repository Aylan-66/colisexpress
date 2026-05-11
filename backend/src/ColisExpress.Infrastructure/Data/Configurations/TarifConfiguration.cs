using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class TarifConfiguration : IEntityTypeConfiguration<Tarif>
{
    public void Configure(EntityTypeBuilder<Tarif> builder)
    {
        builder.ToTable("tarifs");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nom).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.Property(t => t.PrixAuKiloStandard).HasPrecision(10, 2);
        builder.Property(t => t.SeuilStandardKg).HasPrecision(10, 2);
        builder.Property(t => t.ForfaitLourd).HasPrecision(10, 2);
        builder.Property(t => t.PrixAuKiloLourd).HasPrecision(10, 2);
        builder.Property(t => t.ForfaitHorsGabarit).HasPrecision(10, 2);
        builder.Property(t => t.PrixAuKiloHorsGabarit).HasPrecision(10, 2);

        builder.HasOne(t => t.Transporteur)
            .WithMany()
            .HasForeignKey(t => t.TransporteurId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TransporteurId);
    }
}
