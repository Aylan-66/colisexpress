using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class EvenementColisConfiguration : IEntityTypeConfiguration<EvenementColis>
{
    public void Configure(EntityTypeBuilder<EvenementColis> builder)
    {
        builder.ToTable("evenements_colis");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AncienStatut).HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(e => e.NouveauStatut).HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(e => e.DateHeure).IsRequired();
        builder.Property(e => e.Commentaire).HasMaxLength(2000);
        builder.Property(e => e.PhotoChemin).HasMaxLength(512);
        builder.Property(e => e.Latitude).HasPrecision(9, 6);
        builder.Property(e => e.Longitude).HasPrecision(9, 6);

        builder.HasOne(e => e.Colis)
            .WithMany(c => c.Evenements)
            .HasForeignKey(e => e.ColisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Acteur)
            .WithMany()
            .HasForeignKey(e => e.ActeurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ColisId);
        builder.HasIndex(e => e.DateHeure);
    }
}
