using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Domain;
using FluentValidation;

namespace ColisExpress.Application.Validators;

public class CreateCommandeRequestValidator : AbstractValidator<CreateCommandeRequest>
{
    public CreateCommandeRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.TrajetId).NotEmpty();

        RuleFor(x => x.NomDestinataire).NotEmpty().MaximumLength(200)
            .WithMessage("Le nom du destinataire est obligatoire.");
        RuleFor(x => x.TelephoneDestinataire).NotEmpty().MaximumLength(32)
            .WithMessage("Le téléphone du destinataire est obligatoire.");
        RuleFor(x => x.VilleDestinataire).NotEmpty().MaximumLength(150)
            .WithMessage("La ville du destinataire est obligatoire.");

        RuleFor(x => x.DescriptionContenu).NotEmpty().MaximumLength(1000)
            .WithMessage("La description du contenu est obligatoire.");
        RuleFor(x => x.DescriptionContenu)
            .Must(desc => !RulesMetier.ProduitsInterdits.Check(desc ?? "").Interdit)
            .WithMessage(x =>
            {
                var (_, mot) = RulesMetier.ProduitsInterdits.Check(x.DescriptionContenu ?? "");
                return $"Contenu interdit détecté : « {mot} ». Les produits illicites, matières dangereuses, armes et liquides inflammables sont refusés.";
            });
        RuleFor(x => x.PoidsDeclare).GreaterThan(0)
            .WithMessage("Le poids doit être supérieur à 0.");
        RuleFor(x => x.ValeurDeclaree).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dimensions).MaximumLength(50);
        RuleFor(x => x.InstructionsParticulieres).MaximumLength(2000);
    }
}
