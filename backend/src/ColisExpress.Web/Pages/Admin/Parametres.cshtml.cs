using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ColisExpress.Web.Pages.Admin;

public class ParametresModel : PageModel
{
    private readonly IUnitOfWork _uow;
    public ParametresModel(IUnitOfWork uow) => _uow = uow;

    [BindProperty] public string FraisServiceType { get; set; } = "Fixe";
    [BindProperty] public decimal FraisServiceValeur { get; set; }
    public string? Success { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var p = await _uow.GetParametresPlateformeAsync(ct);
        FraisServiceType = p.FraisServiceType.ToString();
        FraisServiceValeur = p.FraisServiceValeur;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!Enum.TryParse<TypeFraisService>(FraisServiceType, out var type))
        {
            Error = "Type invalide.";
            return Page();
        }
        if (FraisServiceValeur < 0)
        {
            Error = "La valeur doit être positive.";
            return Page();
        }

        var p = await _uow.GetParametresPlateformeAsync(ct);
        p.FraisServiceType = type;
        p.FraisServiceValeur = FraisServiceValeur;
        p.DateModification = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        Success = "Frais de service mis à jour.";
        return Page();
    }
}
