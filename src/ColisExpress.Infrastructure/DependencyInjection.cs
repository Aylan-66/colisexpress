using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using ColisExpress.Infrastructure.Repositories;
using ColisExpress.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ColisExpress.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var connectionString = NormalizeConnectionString(raw);

        services.AddDbContext<ColisExpressDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ColisExpressDbContext).Assembly.FullName)));

        services.AddScoped<IUtilisateurRepository, UtilisateurRepository>();
        services.AddScoped<ITransporteurRepository, TransporteurRepository>();
        services.AddScoped<ITrajetRepository, TrajetRepository>();
        services.AddScoped<ICommandeRepository, CommandeRepository>();
        services.AddScoped<IColisRepository, ColisRepository>();
        services.AddScoped<IPaiementRepository, PaiementRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IQrCodeService, Services.QrCodeService>();
        services.AddScoped<IAdminService, Services.AdminService>();
        services.AddScoped<ITransporteurService, Services.TransporteurService>();

        services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        services.AddSingleton<IStripeService, Services.StripeService>();

        return services;
    }

    private static string NormalizeConnectionString(string raw)
    {
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port <= 0 ? 5432 : uri.Port;

            return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Pooling=true";
        }

        return raw;
    }
}
