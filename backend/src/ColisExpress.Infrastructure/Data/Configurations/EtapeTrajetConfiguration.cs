using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class EtapeTrajetConfiguration : IEntityTypeConfiguration<EtapeTrajet>
{
    public void Configure(EntityTypeBuilder<EtapeTrajet> builder)
    {
        builder.ToTable("etapes_trajets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Ordre).IsRequired();
        builder.Property(e => e.HeureEstimeeArrivee).IsRequired();
        builder.Property(e => e.Statut).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.RelaisOuvertALArrivee).IsRequired();

        builder.HasOne(e => e.Trajet)
            .WithMany(t => t.Etapes)
            .HasForeignKey(e => e.TrajetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PointRelais)
            .WithMany()
            .HasForeignKey(e => e.PointRelaisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TrajetId, e.Ordre });
    }
}
