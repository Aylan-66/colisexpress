using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using FluentValidation;

namespace ColisExpress.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _uow = uow;
        _hasher = hasher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return AuthResult.Fail(validation.Errors[0].ErrorMessage);

        var emailNormalise = request.Email.Trim().ToLowerInvariant();

        if (await _uow.Utilisateurs.EmailExistsAsync(emailNormalise, ct))
            return AuthResult.Fail("Un compte existe déjà avec cette adresse email.");

        var utilisateur = new Utilisateur
        {
            Role = request.Role,
            Prenom = request.Prenom.Trim(),
            Nom = request.Nom.Trim(),
            Email = emailNormalise,
            Telephone = request.Telephone.Trim(),
            MotDePasseHash = _hasher.Hash(request.MotDePasse),
            StatutCompte = StatutCompte.Actif,
            EmailVerifie = false
        };

        await _uow.Utilisateurs.AddAsync(utilisateur, ct);
        await _uow.SaveChangesAsync(ct);

        return AuthResult.Ok(utilisateur.Id, utilisateur.Email, utilisateur.Prenom, utilisateur.Nom, utilisateur.Role);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return AuthResult.Fail(validation.Errors[0].ErrorMessage);

        var emailNormalise = request.Email.Trim().ToLowerInvariant();
        var utilisateur = await _uow.Utilisateurs.GetByEmailAsync(emailNormalise, ct);

        if (utilisateur is null || !_hasher.Verify(request.MotDePasse, utilisateur.MotDePasseHash))
            return AuthResult.Fail("Email ou mot de passe incorrect.");

        if (utilisateur.StatutCompte == StatutCompte.Suspendu)
            return AuthResult.Fail("Votre compte est suspendu. Contactez le support.");

        utilisateur.DerniereConnexion = DateTime.UtcNow;
        _uow.Utilisateurs.Update(utilisateur);
        await _uow.SaveChangesAsync(ct);

        return AuthResult.Ok(utilisateur.Id, utilisateur.Email, utilisateur.Prenom, utilisateur.Nom, utilisateur.Role);
    }

    public async Task<AuthResult?> LoginByIdAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var utilisateur = await _uow.Utilisateurs.GetByIdAsync(utilisateurId, ct);
        if (utilisateur is null || utilisateur.StatutCompte == StatutCompte.Suspendu) return null;
        return AuthResult.Ok(utilisateur.Id, utilisateur.Email, utilisateur.Prenom, utilisateur.Nom, utilisateur.Role);
    }
}
