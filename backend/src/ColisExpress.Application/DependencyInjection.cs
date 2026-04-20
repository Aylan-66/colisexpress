using System.Reflection;
using ColisExpress.Application.Interfaces;
using ColisExpress.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ColisExpress.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRechercheService, RechercheService>();
        services.AddScoped<ICommandeService, CommandeService>();
        services.AddScoped<IColisService, ColisService>();
        services.AddScoped<IProfilService, ProfilService>();
        return services;
    }
}
