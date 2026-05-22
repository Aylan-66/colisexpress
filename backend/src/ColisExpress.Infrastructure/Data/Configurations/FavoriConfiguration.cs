using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class FavoriConfiguration : IEntityTypeConfiguration<Favori>
{
    public void Configure(EntityTypeBuilder<Favori> builder)
    {
        builder.ToTable("favoris");
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.Client)
            .WithMany()
            .HasForeignKey(f => f.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.PointRelais)
            .WithMany()
            .HasForeignKey(f => f.PointRelaisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Transporteur)
            .WithMany()
            .HasForeignKey(f => f.TransporteurId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.ClientId);
        builder.HasIndex(f => new { f.ClientId, f.PointRelaisId });
        builder.HasIndex(f => new { f.ClientId, f.TransporteurId });
    }
}
