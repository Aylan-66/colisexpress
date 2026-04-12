using ColisExpress.Application;
using ColisExpress.Infrastructure;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Client", "EstConnecte");
    options.Conventions.AllowAnonymousToPage("/Client/Inscription");
    options.Conventions.AllowAnonymousToPage("/Client/Connexion");
    options.Conventions.AllowAnonymousToPage("/Client/Recherche");
    options.Conventions.AllowAnonymousToPage("/Client/Resultats");
    options.Conventions.AllowAnonymousToPage("/Client/Suivi");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AuthorizeFolder("/Admin", "EstAdmin");
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/connexion";
        options.LogoutPath = "/deconnexion";
        options.AccessDeniedPath = "/connexion";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ColisExpress.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EstConnecte", p => p.RequireAuthenticatedUser());
    options.AddPolicy("EstAdmin", p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "Admin"));
});

var app = builder.Build();

await DbInitializer.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
