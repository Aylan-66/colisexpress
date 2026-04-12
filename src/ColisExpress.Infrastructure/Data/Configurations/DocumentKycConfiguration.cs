using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class DocumentKycConfiguration : IEntityTypeConfiguration<DocumentKyc>
{
    public void Configure(EntityTypeBuilder<DocumentKyc> builder)
    {
        builder.ToTable("documents_kyc");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TypeDocument).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.NomFichier).HasMaxLength(256).IsRequired();
        builder.Property(d => d.CheminFichier).HasMaxLength(512).IsRequired();
        builder.Property(d => d.Statut).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.DateSoumission).IsRequired();

        builder.HasOne(d => d.Transporteur)
            .WithMany(t => t.Documents)
            .HasForeignKey(d => d.TransporteurId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.TransporteurId);
    }
}
