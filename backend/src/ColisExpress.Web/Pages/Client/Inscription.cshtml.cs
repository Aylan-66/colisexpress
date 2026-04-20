using System.Security.Claims;
using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class InscriptionModel : PageModel
{
    private readonly IAuthService _auth;

    public InscriptionModel(IAuthService auth) => _auth = auth;

    [BindProperty]
    public RegisterRequest Input { get; set; } = new();

    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Client/MesCommandes");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Input.Role = RoleUtilisateur.Client;
        var result = await _auth.RegisterAsync(Input, ct);

        if (!result.Success)
        {
            Error = result.Error;
            return Page();
        }

        await SignInAsync(result);
        return RedirectToPage("/Client/MesCommandes");
    }

    private Task SignInAsync(AuthResult result)
    {
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
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
