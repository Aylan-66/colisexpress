using System.Security.Claims;
using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

[AllowAnonymous]
public class ConnexionModel : PageModel
{
    private readonly IAuthService _auth;

    public ConnexionModel(IAuthService auth) => _auth = auth;

    [BindProperty]
    public LoginRequest Input { get; set; } = new();

    public string? Error { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Client/MesCommandes");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await _auth.LoginAsync(Input, ct);

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

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);

        return RedirectToPage("/Client/MesCommandes");
    }
}
