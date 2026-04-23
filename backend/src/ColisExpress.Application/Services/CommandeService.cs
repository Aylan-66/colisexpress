using ColisExpress.Application.DTOs.Commandes;
using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Domain.Exceptions;
using ColisExpress.Domain.Interfaces;
using FluentValidation;

namespace ColisExpress.Application.Services;

public class CommandeService : ICommandeService
{
    private readonly IUnitOfWork _uow;
    private readonly IQrCodeService _qr;
    private readonly IValidator<CreateCommandeRequest> _validator;

    public CommandeService(IUnitOfWork uow, IQrCodeService qr, IValidator<CreateCommandeRequest> validator)
    {
        _uow = uow;
        _qr = qr;
        _validator = validator;
    }

    public async Task<CommandeResponse> CreateAsync(CreateCommandeRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var trajet = await _uow.Trajets.GetByIdAsync(request.TrajetId, ct)
            ?? throw new DomainException("Trajet introuvable.");

        if (trajet.Statut != StatutTrajet.Actif)
            throw new DomainException("Ce trajet n'est plus actif.");

        if (trajet.CapaciteRestante <= 0)
            throw new DomainException("Ce trajet est complet.");

        if (request.PoidsDeclare > trajet.CapaciteMaxPoids)
            throw new DomainException($"Le poids dépasse la capacité maximale ({trajet.CapaciteMaxPoids} kg).");

        var prixTransport = RechercheService.CalculerPrix(trajet, request.PoidsDeclare, request.Urgent, request.Fragile);
        const decimal fraisService = 5m;
        var supplements = 0m;
        if (request.Urgent) supplements += trajet.SupplementUrgent ?? 0;
        if (request.Fragile) supplements += trajet.SupplementFragile ?? 0;
        var total = prixTransport + fraisService;

        var commande = new Commande
        {
            ClientId = request.ClientId,
            TransporteurId = trajet.TransporteurId,
            TrajetId = trajet.Id,
            NomDestinataire = request.NomDestinataire.Trim(),
            TelephoneDestinataire = request.TelephoneDestinataire.Trim(),
            VilleDestinataire = request.VilleDestinataire.Trim(),
            DescriptionContenu = request.DescriptionContenu.Trim(),
            PoidsDeclare = request.PoidsDeclare,
            Dimensions = request.Dimensions,
            ValeurDeclaree = request.ValeurDeclaree,
            PrixTransport = prixTransport,
            FraisService = fraisService,
            SupplementsTotal = supplements,
            Total = total,
            ModeReglement = request.ModeReglement,
            StatutReglement = StatutReglement.EnAttente,
            InstructionsParticulieres = request.InstructionsParticulieres
        };

        var codeColis = await GenerateCodeColisAsync(ct);
        var codeRetrait = Random.Shared.Next(1000, 10000).ToString();

        var statutInitial = request.ModeReglement == ModeReglement.Especes
            ? StatutColis.EnAttenteReglement
            : StatutColis.DemandeCreee;

        var colis = new Colis
        {
            CommandeId = commande.Id,
            CodeColis = codeColis,
            CodeRetrait = codeRetrait,
            QrCodeData = codeColis,
            Statut = statutInitial
        };

        var evenement = new EvenementColis
        {
            ColisId = colis.Id,
            AncienStatut = StatutColis.Brouillon,
            NouveauStatut = StatutColis.DemandeCreee,
            ActeurId = request.ClientId,
            Commentaire = "Commande créée par le client"
        };
        colis.Evenements.Add(evenement);

        trajet.CapaciteRestante -= 1;
        if (trajet.CapaciteRestante == 0) trajet.Statut = StatutTrajet.Complet;

        await _uow.Commandes.AddAsync(commande, ct);
        await _uow.Colis.AddAsync(colis, ct);
        await _uow.SaveChangesAsync(ct);

        return await ToCommandeResponseAsync(commande.Id, ct) ?? throw new DomainException("Erreur création commande.");
    }

    public async Task<CommandeResponse?> GetByIdAsync(Guid commandeId, Guid clientId, CancellationToken ct = default)
    {
        var commande = await _uow.Commandes.GetByIdAsync(commandeId, ct);
        if (commande is null || commande.ClientId != clientId) return null;
        return await ToCommandeResponseAsync(commande.Id, ct);
    }

    public async Task<IReadOnlyList<CommandeListItem>> GetCommandesClientAsync(Guid clientId, FiltreCommandes filtre = FiltreCommandes.Toutes, CancellationToken ct = default)
    {
        var commandes = await _uow.Commandes.GetByClientIdAsync(clientId, ct);

        var filtrees = filtre switch
        {
            FiltreCommandes.Livrees => commandes.Where(c => c.Colis?.Statut == StatutColis.LivraisonCloturee || c.Colis?.Statut == StatutColis.RetireParDestinataire),
            FiltreCommandes.Annulees => commandes.Where(c => c.Colis?.Statut == StatutColis.Annulee),
            FiltreCommandes.EnCours => commandes.Where(c =>
                c.Colis?.Statut != StatutColis.LivraisonCloturee &&
                c.Colis?.Statut != StatutColis.RetireParDestinataire &&
                c.Colis?.Statut != StatutColis.Annulee &&
                c.Colis?.Statut != StatutColis.Perdu),
            _ => commandes.AsEnumerable()
        };

        var result = new List<CommandeListItem>();
        foreach (var c in filtrees)
        {
            var transporteur = await _uow.Transporteurs.GetByIdAsync(c.TransporteurId, ct);
            var utilisateur = transporteur is null ? null : await _uow.Utilisateurs.GetByIdAsync(transporteur.UtilisateurId, ct);
            var nomT = utilisateur is null ? "—" : $"{utilisateur.Prenom} {utilisateur.Nom[..1]}.";
            result.Add(new CommandeListItem
            {
                Id = c.Id,
                CodeColis = c.Colis?.CodeColis ?? "",
                Trajet = $"{c.Trajet?.VilleDepart} → {c.Trajet?.VilleArrivee}",
                NomTransporteur = nomT,
                StatutColis = c.Colis?.Statut ?? StatutColis.Brouillon,
                Total = c.Total,
                DateCreation = c.DateCreation
            });
        }
        return result;
    }

    public async Task<CommandeDetailResponse?> GetDetailAsync(Guid commandeId, Guid clientId, CancellationToken ct = default)
    {
        var commande = await _uow.Commandes.GetByIdAsync(commandeId, ct);
        if (commande is null || commande.ClientId != clientId) return null;

        var transporteur = await _uow.Transporteurs.GetByIdAsync(commande.TransporteurId, ct);
        var utilisateurT = transporteur is null ? null : await _uow.Utilisateurs.GetByIdAsync(transporteur.UtilisateurId, ct);
        var prenom = utilisateurT?.Prenom ?? "";
        var nom = utilisateurT?.Nom ?? "";
        var initiales = (prenom.Length > 0 ? prenom[..1] : "") + (nom.Length > 0 ? nom[..1] : "");

        var statutColis = commande.Colis?.Statut ?? StatutColis.Brouillon;

        return new CommandeDetailResponse
        {
            Id = commande.Id,
            CodeColis = commande.Colis?.CodeColis ?? "",
            CodeRetrait = commande.Colis?.CodeRetrait ?? "",
            StatutColis = statutColis,
            StatutReglement = commande.StatutReglement,
            EstAnnulable = Domain.RulesMetier.Annulation.EstAnnulable(statutColis),
            VilleDepart = commande.Trajet?.VilleDepart ?? "",
            VilleArrivee = commande.Trajet?.VilleArrivee ?? "",
            DateDepart = commande.Trajet?.DateDepart ?? DateTime.UtcNow,
            DateEstimeeArrivee = commande.Trajet?.DateEstimeeArrivee ?? DateTime.UtcNow,
            NomTransporteur = $"{prenom} {nom}".Trim(),
            Initiales = initiales.ToUpperInvariant(),
            NoteTransporteur = transporteur?.NoteMoyenne ?? 0,
            NbAvisTransporteur = transporteur?.NombreAvis ?? 0,
            TypeVehicule = transporteur?.TypeVehicule,
            NomDestinataire = commande.NomDestinataire,
            TelephoneDestinataire = commande.TelephoneDestinataire,
            VilleDestinataire = commande.VilleDestinataire,
            DescriptionContenu = commande.DescriptionContenu,
            PoidsDeclare = commande.PoidsDeclare,
            Dimensions = commande.Dimensions,
            ValeurDeclaree = commande.ValeurDeclaree,
            InstructionsParticulieres = commande.InstructionsParticulieres,
            PrixTransport = commande.PrixTransport,
            FraisService = commande.FraisService,
            SupplementsTotal = commande.SupplementsTotal,
            Total = commande.Total,
            DateCreation = commande.DateCreation
        };
    }

    public async Task<OperationResult> AnnulerAsync(Guid commandeId, Guid clientId, CancellationToken ct = default)
    {
        var commande = await _uow.Commandes.GetByIdAsync(commandeId, ct);
        if (commande is null || commande.ClientId != clientId)
            return OperationResult.Fail("Commande introuvable.");

        var statut = commande.Colis?.Statut ?? StatutColis.Brouillon;
        if (!Domain.RulesMetier.Annulation.EstAnnulable(statut))
            return OperationResult.Fail($"Impossible d'annuler une commande au statut « {statut} ». Contactez le support.");

        var trajet = await _uow.Trajets.GetByIdAsync(commande.TrajetId, ct);
        if (trajet is not null)
        {
            trajet.CapaciteRestante += 1;
            if (trajet.Statut == StatutTrajet.Complet) trajet.Statut = StatutTrajet.Actif;
        }

        if (commande.Colis is not null)
        {
            var ancien = commande.Colis.Statut;
            commande.Colis.Statut = StatutColis.Annulee;
            await _uow.Colis.AddEvenementAsync(new EvenementColis
            {
                ColisId = commande.Colis.Id,
                AncienStatut = ancien,
                NouveauStatut = StatutColis.Annulee,
                ActeurId = clientId,
                Commentaire = "Commande annulée par le client"
            }, ct);
        }

        if (commande.StatutReglement == StatutReglement.Paye)
        {
            commande.StatutReglement = StatutReglement.Rembourse;
            var remboursement = new Paiement
            {
                CommandeId = commande.Id,
                Mode = commande.ModeReglement,
                Montant = -commande.Total,
                Statut = StatutReglement.Rembourse,
                DateEncaissement = DateTime.UtcNow,
                ReferenceExterne = "Remboursement annulation"
            };
            await _uow.Paiements.AddAsync(remboursement, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task ConfirmerPaiementAsync(Guid commandeId, Guid clientId, string? referenceExterne = null, CancellationToken ct = default)
    {
        var commande = await _uow.Commandes.GetByIdAsync(commandeId, ct)
            ?? throw new DomainException("Commande introuvable.");
        if (commande.ClientId != clientId)
            throw new DomainException("Accès refusé.");

        if (commande.StatutReglement == StatutReglement.Paye)
            return;

        commande.StatutReglement = StatutReglement.Paye;

        var paiement = new Paiement
        {
            CommandeId = commande.Id,
            Mode = commande.ModeReglement,
            Montant = commande.Total,
            Statut = StatutReglement.Paye,
            DateEncaissement = DateTime.UtcNow,
            ReferenceExterne = referenceExterne
        };
        await _uow.Paiements.AddAsync(paiement, ct);

        if (commande.Colis is not null)
        {
            var ancien = commande.Colis.Statut;
            commande.Colis.Statut = StatutColis.EnAttenteDepot;

            await _uow.Colis.AddEvenementAsync(new EvenementColis
            {
                ColisId = commande.Colis.Id,
                AncienStatut = ancien,
                NouveauStatut = StatutColis.EnAttenteDepot,
                ActeurId = clientId,
                Commentaire = "Paiement confirmé — en attente de dépôt au point relais"
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CommandeListItem>> GetCommandesTransporteurAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var transporteur = await _uow.Transporteurs.GetByUtilisateurIdAsync(utilisateurId, ct);
        if (transporteur is null) return Array.Empty<CommandeListItem>();

        var commandes = await _uow.Commandes.GetByTransporteurIdAsync(transporteur.Id, ct);
        var result = new List<CommandeListItem>();
        foreach (var c in commandes)
        {
            var client = await _uow.Utilisateurs.GetByIdAsync(c.ClientId, ct);
            var nomClient = client is null ? "—" : $"{client.Prenom} {client.Nom[..1]}.";
            result.Add(new CommandeListItem
            {
                Id = c.Id,
                CodeColis = c.Colis?.CodeColis ?? "",
                Trajet = $"{c.Trajet?.VilleDepart} → {c.Trajet?.VilleArrivee}",
                NomTransporteur = nomClient,
                StatutColis = c.Colis?.Statut ?? StatutColis.Brouillon,
                Total = c.Total,
                DateCreation = c.DateCreation
            });
        }
        return result;
    }

    private async Task<string> GenerateCodeColisAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        for (int i = 0; i < 10; i++)
        {
            var num = Random.Shared.Next(1, 10000).ToString("D4");
            var code = $"COL-{year}-{num}";
            if (!await _uow.Colis.CodeColisExistsAsync(code, ct))
                return code;
        }
        return $"COL-{year}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }

    private async Task<CommandeResponse?> ToCommandeResponseAsync(Guid commandeId, CancellationToken ct)
    {
        var commande = await _uow.Commandes.GetByIdAsync(commandeId, ct);
        if (commande is null) return null;

        var transporteur = await _uow.Transporteurs.GetByIdAsync(commande.TransporteurId, ct);
        var utilisateurT = transporteur is null ? null : await _uow.Utilisateurs.GetByIdAsync(transporteur.UtilisateurId, ct);
        var nomT = utilisateurT is null ? "" : $"{utilisateurT.Prenom} {utilisateurT.Nom}";

        return new CommandeResponse
        {
            Id = commande.Id,
            CodeColis = commande.Colis?.CodeColis ?? "",
            CodeRetrait = commande.Colis?.CodeRetrait ?? "",
            PrixTransport = commande.PrixTransport,
            FraisService = commande.FraisService,
            SupplementsTotal = commande.SupplementsTotal,
            Total = commande.Total,
            StatutColis = commande.Colis?.Statut ?? StatutColis.Brouillon,
            StatutReglement = commande.StatutReglement,
            VilleDepart = commande.Trajet?.VilleDepart ?? "",
            VilleArrivee = commande.Trajet?.VilleArrivee ?? "",
            DateDepart = commande.Trajet?.DateDepart ?? DateTime.UtcNow,
            NomTransporteur = nomT,
            DateCreation = commande.DateCreation
        };
    }
}
