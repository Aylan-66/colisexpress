using System.Security.Claims;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class InscriptionTransporteurModel : PageModel
{
    private readonly ITransporteurService _transporteur;

    public InscriptionTransporteurModel(ITransporteurService transporteur) => _transporteur = transporteur;

    [BindProperty] public RegisterTransporteurRequest Input { get; set; } = new();
    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Client/Profil");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromForm(Name = "corridors")] string[]? corridors, CancellationToken ct)
    {
        Input.CorridorsActifs = corridors is { Length: > 0 } ? string.Join(",", corridors) : "";

        var result = await _transporteur.RegisterAsync(Input, ct);
        if (!result.Success)
        {
            Error = result.Error;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UtilisateurId.ToString()),
            new(ClaimTypes.Email, result.Email),
            new(ClaimTypes.Name, $"{result.Prenom} {result.Nom}"),
            new(ClaimTypes.Role, result.Role.ToString()),
            new("Prenom", result.Prenom),
            new("Nom", result.Nom)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToPage("/Transporteur/Kyc");
    }
}
