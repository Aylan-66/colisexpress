using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class PaiementConfiguration : IEntityTypeConfiguration<Paiement>
{
    public void Configure(EntityTypeBuilder<Paiement> builder)
    {
        builder.ToTable("paiements");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Mode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Montant).HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.Statut).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.ReferenceExterne).HasMaxLength(256);
        builder.Property(p => p.DateCreation).IsRequired();

        builder.HasOne(p => p.Commande)
            .WithMany()
            .HasForeignKey(p => p.CommandeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.CommandeId);
        builder.HasIndex(p => p.Statut);
    }
}
