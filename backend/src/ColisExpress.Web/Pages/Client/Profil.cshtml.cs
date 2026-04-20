using System.Security.Claims;
using ColisExpress.Application.DTOs.Profil;
using ColisExpress.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Client;

public class ProfilModel : PageModel
{
    private readonly IProfilService _profil;

    public ProfilModel(IProfilService profil) => _profil = profil;

    [BindProperty] public UpdateProfilRequest Input { get; set; } = new();
    [BindProperty] public ChangerMotDePasseRequest ChangePwd { get; set; } = new();

    public ProfilStatsResponse Stats { get; private set; } = new();
    public string Email { get; private set; } = "";
    public string Role { get; private set; } = "";
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var id = GetUserId();
        if (id is null) return Challenge();
        await LoadAsync(id.Value, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken ct)
    {
        var id = GetUserId();
        if (id is null) return Challenge();

        var result = await _profil.UpdateAsync(id.Value, Input, ct);
        if (!result.Success)
        {
            Error = result.Error;
            await LoadAsync(id.Value, ct);
            return Page();
        }

        await RefreshCookieAsync();
        Success = "Profil mis à jour.";
        await LoadAsync(id.Value, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(CancellationToken ct)
    {
        var id = GetUserId();
        if (id is null) return Challenge();

        var result = await _profil.ChangerMotDePasseAsync(id.Value, ChangePwd, ct);
        if (!result.Success)
        {
            Error = result.Error;
            await LoadAsync(id.Value, ct);
            return Page();
        }

        Success = "Mot de passe modifié.";
        await LoadAsync(id.Value, ct);
        return Page();
    }

    private async Task LoadAsync(Guid id, CancellationToken ct)
    {
        Input = await _profil.GetProfilAsync(id, ct) ?? new UpdateProfilRequest();
        Stats = await _profil.GetStatsAsync(id, ct);
        Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        Role = User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    private Guid? GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(s, out var id) ? id : null;
    }

    private async Task RefreshCookieAsync()
    {
        var identity = (ClaimsIdentity)User.Identity!;
        var claimPrenom = identity.FindFirst("Prenom");
        if (claimPrenom is not null) identity.RemoveClaim(claimPrenom);
        identity.AddClaim(new Claim("Prenom", Input.Prenom));

        var claimNom = identity.FindFirst("Nom");
        if (claimNom is not null) identity.RemoveClaim(claimNom);
        identity.AddClaim(new Claim("Nom", Input.Nom));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
