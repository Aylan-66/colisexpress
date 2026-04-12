using ColisExpress.Application.DTOs.Profil;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using FluentValidation;

namespace ColisExpress.Application.Services;

public class ProfilService : IProfilService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IValidator<UpdateProfilRequest> _updateValidator;
    private readonly IValidator<ChangerMotDePasseRequest> _changePwdValidator;

    public ProfilService(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IValidator<UpdateProfilRequest> updateValidator,
        IValidator<ChangerMotDePasseRequest> changePwdValidator)
    {
        _uow = uow;
        _hasher = hasher;
        _updateValidator = updateValidator;
        _changePwdValidator = changePwdValidator;
    }

    public async Task<UpdateProfilRequest?> GetProfilAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var u = await _uow.Utilisateurs.GetByIdAsync(utilisateurId, ct);
        if (u is null) return null;
        return new UpdateProfilRequest
        {
            Prenom = u.Prenom,
            Nom = u.Nom,
            Telephone = u.Telephone,
            Adresse = u.Adresse
        };
    }

    public async Task<OperationResult> UpdateAsync(Guid utilisateurId, UpdateProfilRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return OperationResult.Fail(validation.Errors[0].ErrorMessage);

        var u = await _uow.Utilisateurs.GetByIdAsync(utilisateurId, ct);
        if (u is null) return OperationResult.Fail("Utilisateur introuvable.");

        u.Prenom = request.Prenom.Trim();
        u.Nom = request.Nom.Trim();
        u.Telephone = request.Telephone.Trim();
        u.Adresse = request.Adresse?.Trim();

        _uow.Utilisateurs.Update(u);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ChangerMotDePasseAsync(Guid utilisateurId, ChangerMotDePasseRequest request, CancellationToken ct = default)
    {
        var validation = await _changePwdValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return OperationResult.Fail(validation.Errors[0].ErrorMessage);

        var u = await _uow.Utilisateurs.GetByIdAsync(utilisateurId, ct);
        if (u is null) return OperationResult.Fail("Utilisateur introuvable.");

        if (!_hasher.Verify(request.AncienMotDePasse, u.MotDePasseHash))
            return OperationResult.Fail("Ancien mot de passe incorrect.");

        u.MotDePasseHash = _hasher.Hash(request.NouveauMotDePasse);
        _uow.Utilisateurs.Update(u);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<ProfilStatsResponse> GetStatsAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var commandes = await _uow.Commandes.GetByClientIdAsync(utilisateurId, ct);
        var utilisateur = await _uow.Utilisateurs.GetByIdAsync(utilisateurId, ct);

        var anneeEnCours = DateTime.UtcNow.Year;
        var montantAnnee = commandes
            .Where(c => c.DateCreation.Year == anneeEnCours
                && c.StatutReglement == Domain.Enums.StatutReglement.Paye
                && c.Colis?.Statut != StatutColis.Annulee)
            .Sum(c => c.Total);

        return new ProfilStatsResponse
        {
            EnvoisTotaux = commandes.Count(c => c.Colis?.Statut != StatutColis.Annulee),
            Livres = commandes.Count(c => c.Colis?.Statut == StatutColis.LivraisonCloturee),
            MembreDepuis = utilisateur?.DateCreation ?? DateTime.UtcNow,
            MontantAnneeEnCours = montantAnnee,
            PlafondCotransportage = Domain.RulesMetier.Cotransportage.PlafondAnnuelEuros,
            AlerteCotransportage = montantAnnee >= Domain.RulesMetier.Cotransportage.SeuilAlerte
        };
    }
}
