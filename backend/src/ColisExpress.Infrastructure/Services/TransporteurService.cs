using ColisExpress.Application.DTOs.Auth;
using ColisExpress.Application.DTOs.Trajets;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Interfaces;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Services;

public class TransporteurService : ITransporteurService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ColisExpressDbContext _db;

    public TransporteurService(IUnitOfWork uow, IPasswordHasher hasher, ColisExpressDbContext db)
    {
        _uow = uow;
        _hasher = hasher;
        _db = db;
    }

    public async Task<AuthResult> RegisterAsync(RegisterTransporteurRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prenom) || string.IsNullOrWhiteSpace(request.Nom))
            return AuthResult.Fail("Prénom et nom sont obligatoires.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return AuthResult.Fail("Email invalide.");
        if (request.MotDePasse.Length < 8)
            return AuthResult.Fail("Le mot de passe doit contenir au moins 8 caractères.");
        if (request.MotDePasse != request.ConfirmationMotDePasse)
            return AuthResult.Fail("Les mots de passe ne correspondent pas.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _uow.Utilisateurs.EmailExistsAsync(email, ct))
            return AuthResult.Fail("Un compte existe déjà avec cette adresse email.");

        var utilisateur = new Utilisateur
        {
            Role = RoleUtilisateur.Transporteur,
            Prenom = request.Prenom.Trim(),
            Nom = request.Nom.Trim(),
            Email = email,
            Telephone = request.Telephone.Trim(),
            MotDePasseHash = _hasher.Hash(request.MotDePasse),
            StatutCompte = StatutCompte.Actif,
            EmailVerifie = false
        };

        await _uow.Utilisateurs.AddAsync(utilisateur, ct);
        await _uow.SaveChangesAsync(ct);

        var transporteur = new Transporteur
        {
            UtilisateurId = utilisateur.Id,
            StatutKyc = StatutKyc.NonSoumis,
            TypeVehicule = request.TypeVehicule.Trim(),
            CorridorsActifs = request.CorridorsActifs.Trim()
        };

        await _uow.Transporteurs.AddAsync(transporteur, ct);
        await _uow.SaveChangesAsync(ct);

        return AuthResult.Ok(utilisateur.Id, utilisateur.Email, utilisateur.Prenom, utilisateur.Nom, utilisateur.Role);
    }

    public async Task<TransporteurDashboardResponse?> GetDashboardAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var transporteur = await _uow.Transporteurs.GetByUtilisateurIdAsync(utilisateurId, ct);
        if (transporteur is null) return null;

        var docs = await _db.DocumentsKyc
            .Where(d => d.TransporteurId == transporteur.Id)
            .OrderBy(d => d.TypeDocument)
            .Select(d => new DocumentKycItem
            {
                Id = d.Id,
                TypeDocument = d.TypeDocument,
                NomFichier = d.NomFichier,
                Statut = d.Statut,
                DateSoumission = d.DateSoumission
            })
            .ToListAsync(ct);

        return new TransporteurDashboardResponse
        {
            TransporteurId = transporteur.Id,
            StatutKyc = transporteur.StatutKyc,
            Documents = docs,
            PeutPublierOffres = transporteur.StatutKyc == StatutKyc.Valide
        };
    }

    public async Task<OperationResult> UploadDocumentAsync(UploadDocumentKycRequest request, CancellationToken ct = default)
    {
        var transporteur = await _db.Transporteurs.FirstOrDefaultAsync(t => t.Id == request.TransporteurId, ct);
        if (transporteur is null) return OperationResult.Fail("Transporteur introuvable.");

        if (request.Contenu.Length > 5 * 1024 * 1024)
            return OperationResult.Fail("Le fichier est trop volumineux (5 Mo max).");

        var existing = await _db.DocumentsKyc
            .FirstOrDefaultAsync(d => d.TransporteurId == request.TransporteurId && d.TypeDocument == request.TypeDocument, ct);

        if (existing is not null)
        {
            existing.NomFichier = request.NomFichier;
            existing.ContentType = request.ContentType;
            existing.ContenuFichier = request.Contenu;
            existing.CheminFichier = $"db://{existing.Id}";
            existing.Statut = StatutKyc.EnAttente;
            existing.DateSoumission = DateTime.UtcNow;
            existing.DateValidation = null;
            existing.ValidePar = null;
        }
        else
        {
            var doc = new DocumentKyc
            {
                TransporteurId = request.TransporteurId,
                TypeDocument = request.TypeDocument,
                NomFichier = request.NomFichier,
                ContentType = request.ContentType,
                ContenuFichier = request.Contenu,
                CheminFichier = "db://pending",
                Statut = StatutKyc.EnAttente
            };
            await _db.DocumentsKyc.AddAsync(doc, ct);
        }

        if (transporteur.StatutKyc == StatutKyc.NonSoumis || transporteur.StatutKyc == StatutKyc.Rejete)
            transporteur.StatutKyc = StatutKyc.EnAttente;

        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<(byte[]? Contenu, string? ContentType, string? NomFichier)> GetDocumentFileAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.DocumentsKyc.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc?.ContenuFichier is null) return (null, null, null);
        return (doc.ContenuFichier, doc.ContentType, doc.NomFichier);
    }
}
