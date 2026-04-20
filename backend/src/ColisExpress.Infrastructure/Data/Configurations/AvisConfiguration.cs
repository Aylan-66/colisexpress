using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class AvisConfiguration : IEntityTypeConfiguration<Avis>
{
    public void Configure(EntityTypeBuilder<Avis> builder)
    {
        builder.ToTable("avis");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Note).IsRequired();
        builder.Property(a => a.Commentaire).HasMaxLength(2000);
        builder.Property(a => a.DateCreation).IsRequired();

        builder.HasOne(a => a.Commande)
            .WithMany()
            .HasForeignKey(a => a.CommandeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Client)
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Transporteur)
            .WithMany()
            .HasForeignKey(a => a.TransporteurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.CommandeId).IsUnique();
        builder.HasIndex(a => a.TransporteurId);
    }
}
