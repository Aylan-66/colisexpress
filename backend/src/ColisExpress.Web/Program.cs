using System.Text;
using ColisExpress.Application;
using ColisExpress.Application.Interfaces;
using ColisExpress.Infrastructure;
using ColisExpress.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // Sérialise les enums en chaînes (ex: "DeposeParClient") au lieu d'entiers,
    // pour que les apps mobiles filtrent et comparent les statuts par nom.
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Client", "EstConnecte");
    options.Conventions.AllowAnonymousToPage("/Client/Inscription");
    options.Conventions.AllowAnonymousToPage("/Client/InscriptionTransporteur");
    options.Conventions.AllowAnonymousToPage("/Client/Connexion");
    // Recherche / Résultats / Suivi nécessitent désormais d'être connecté (demande client)
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Client/QuiSommesNous");
    options.Conventions.AllowAnonymousToPage("/Client/Recu");
    options.Conventions.AuthorizeFolder("/Admin", "EstAdmin");
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpClient(); // pour IHttpClientFactory (géocodage Nominatim)

var jwtKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey)) jwtKey = "ColisExpressDefaultDevKeyMinimum32Chars!!";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
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
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ColisExpress",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ColisExpressApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EstConnecte", p => p.RequireAuthenticatedUser());
    options.AddPolicy("EstAdmin", p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "Admin"));
    options.AddPolicy("EstTransporteur", p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "Transporteur"));
    options.AddPolicy("EstPointRelais", p => p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "PointRelais"));
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
app.MapControllers();

app.Run();
