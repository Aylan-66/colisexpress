using ColisExpress.Application.DTOs.Profil;
using FluentValidation;

namespace ColisExpress.Application.Validators;

public class UpdateProfilRequestValidator : AbstractValidator<UpdateProfilRequest>
{
    public UpdateProfilRequestValidator()
    {
        RuleFor(x => x.Prenom).NotEmpty().MaximumLength(100).WithMessage("Le prénom est obligatoire.");
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(100).WithMessage("Le nom est obligatoire.");
        RuleFor(x => x.Telephone).NotEmpty().MaximumLength(32).WithMessage("Le téléphone est obligatoire.");
        RuleFor(x => x.Adresse).MaximumLength(500);
    }
}

public class ChangerMotDePasseRequestValidator : AbstractValidator<ChangerMotDePasseRequest>
{
    public ChangerMotDePasseRequestValidator()
    {
        RuleFor(x => x.AncienMotDePasse).NotEmpty().WithMessage("L'ancien mot de passe est obligatoire.");
        RuleFor(x => x.NouveauMotDePasse).NotEmpty().MinimumLength(8)
            .WithMessage("Le nouveau mot de passe doit contenir au moins 8 caractères.");
        RuleFor(x => x.ConfirmationNouveauMotDePasse).Equal(x => x.NouveauMotDePasse)
            .WithMessage("Les mots de passe ne correspondent pas.");
    }
}
