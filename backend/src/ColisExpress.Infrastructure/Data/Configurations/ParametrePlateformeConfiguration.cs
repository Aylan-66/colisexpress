using ColisExpress.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ColisExpress.Infrastructure.Data.Configurations;

public class ParametrePlateformeConfiguration : IEntityTypeConfiguration<ParametrePlateforme>
{
    public void Configure(EntityTypeBuilder<ParametrePlateforme> builder)
    {
        builder.ToTable("parametres_plateforme");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FraisServiceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.FraisServiceValeur).HasPrecision(10, 2);
    }
}
