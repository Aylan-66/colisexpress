using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("utilisateurs");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(u => u.Nom).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Telephone).HasMaxLength(32).IsRequired();
        builder.Property(u => u.MotDePasseHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Adresse).HasMaxLength(500);
        builder.Property(u => u.StatutCompte).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(u => u.EmailVerifie).IsRequired();
        builder.Property(u => u.DateCreation).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Role);
    }
}
