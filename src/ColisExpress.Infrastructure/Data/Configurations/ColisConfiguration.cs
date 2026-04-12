using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class ColisConfiguration : IEntityTypeConfiguration<Colis>
{
    public void Configure(EntityTypeBuilder<Colis> builder)
    {
        builder.ToTable("colis");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CodeColis).HasMaxLength(32).IsRequired();
        builder.Property(c => c.QrCodeData).HasMaxLength(512).IsRequired();
        builder.Property(c => c.CodeRetrait).HasMaxLength(16).IsRequired();
        builder.Property(c => c.PoidsReel).HasPrecision(10, 2);
        builder.Property(c => c.Statut).HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(c => c.DateCreation).IsRequired();

        builder.HasOne(c => c.Commande)
            .WithOne(cmd => cmd.Colis!)
            .HasForeignKey<Colis>(c => c.CommandeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CodeColis).IsUnique();
        builder.HasIndex(c => c.CommandeId).IsUnique();
        builder.HasIndex(c => c.Statut);
    }
}
