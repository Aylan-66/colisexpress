using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class CommandeConfiguration : IEntityTypeConfiguration<Commande>
{
    public void Configure(EntityTypeBuilder<Commande> builder)
    {
        builder.ToTable("commandes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NomDestinataire).HasMaxLength(200).IsRequired();
        builder.Property(c => c.TelephoneDestinataire).HasMaxLength(32).IsRequired();
        builder.Property(c => c.VilleDestinataire).HasMaxLength(150).IsRequired();

        builder.Property(c => c.DescriptionContenu).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.PoidsDeclare).HasPrecision(10, 2).IsRequired();
        builder.Property(c => c.Dimensions).HasMaxLength(50);
        builder.Property(c => c.ValeurDeclaree).HasPrecision(12, 2).IsRequired();

        builder.Property(c => c.PrixTransport).HasPrecision(12, 2).IsRequired();
        builder.Property(c => c.FraisService).HasPrecision(12, 2).IsRequired();
        builder.Property(c => c.SupplementsTotal).HasPrecision(12, 2).IsRequired();
        builder.Property(c => c.Total).HasPrecision(12, 2).IsRequired();

        builder.Property(c => c.ModeReglement).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.StatutReglement).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(c => c.InstructionsParticulieres).HasMaxLength(2000);
        builder.Property(c => c.DateCreation).IsRequired();

        builder.HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Transporteur)
            .WithMany()
            .HasForeignKey(c => c.TransporteurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Trajet)
            .WithMany()
            .HasForeignKey(c => c.TrajetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.RelaisDepart)
            .WithMany()
            .HasForeignKey(c => c.RelaisDepartId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.RelaisArrivee)
            .WithMany()
            .HasForeignKey(c => c.RelaisArriveeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ClientId);
        builder.HasIndex(c => c.TransporteurId);
        builder.HasIndex(c => c.TrajetId);
        builder.HasIndex(c => c.DateCreation);
    }
}
