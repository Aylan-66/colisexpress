using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class TransporteurConfiguration : IEntityTypeConfiguration<Transporteur>
{
    public void Configure(EntityTypeBuilder<Transporteur> builder)
    {
        builder.ToTable("transporteurs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.StatutKyc).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.NoteMoyenne).HasPrecision(3, 2);
        builder.Property(t => t.TypeVehicule).HasMaxLength(100);
        builder.Property(t => t.CorridorsActifs).HasMaxLength(200);

        builder.HasOne(t => t.Utilisateur)
            .WithOne()
            .HasForeignKey<Transporteur>(t => t.UtilisateurId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UtilisateurId).IsUnique();
        builder.HasIndex(t => t.StatutKyc);
    }
}
