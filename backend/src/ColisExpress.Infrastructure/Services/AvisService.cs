using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using ColisExpress.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ColisExpress.Infrastructure.Services;

public class AvisService : IAvisService
{
    private readonly ColisExpressDbContext _db;

    public AvisService(ColisExpressDbContext db) => _db = db;

    public async Task<OperationResult> CreateAsync(CreateAvisRequest request, CancellationToken ct = default)
    {
        if (request.Note < 1 || request.Note > 5)
            return OperationResult.Fail("La note doit être entre 1 et 5.");

        var commande = await _db.Commandes
            .Include(c => c.Colis)
            .FirstOrDefaultAsync(c => c.Id == request.CommandeId && c.ClientId == request.ClientId, ct);

        if (commande is null) return OperationResult.Fail("Commande introuvable.");

        var statutColis = commande.Colis?.Statut ?? StatutColis.Brouillon;
        if (statutColis != StatutColis.LivraisonCloturee && statutColis != StatutColis.RetireParDestinataire &&
            statutColis != StatutColis.ReservationConfirmee && statutColis != StatutColis.DisponibleAuRetrait)
            return OperationResult.Fail("Vous ne pouvez laisser un avis que sur une commande complétée.");

        if (await _db.Avis.AnyAsync(a => a.CommandeId == request.CommandeId, ct))
            return OperationResult.Fail("Vous avez déjà laissé un avis pour cette commande.");

        var avis = new Avis
        {
            CommandeId = request.CommandeId,
            ClientId = request.ClientId,
            TransporteurId = commande.TransporteurId,
            Note = request.Note,
            Commentaire = request.Commentaire?.Trim()
        };
        await _db.Avis.AddAsync(avis, ct);

        var transporteur = await _db.Transporteurs.FirstOrDefaultAsync(t => t.Id == commande.TransporteurId, ct);
        if (transporteur is not null)
        {
            var totalNotes = transporteur.NoteMoyenne * transporteur.NombreAvis + request.Note;
            transporteur.NombreAvis += 1;
            transporteur.NoteMoyenne = Math.Round(totalNotes / transporteur.NombreAvis, 2);
        }

        await _db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    public async Task<AvisResponse?> GetByCommandeIdAsync(Guid commandeId, CancellationToken ct = default)
    {
        return await _db.Avis
            .Include(a => a.Client)
            .Where(a => a.CommandeId == commandeId)
            .Select(a => new AvisResponse
            {
                Id = a.Id,
                Note = a.Note,
                Commentaire = a.Commentaire,
                NomClient = a.Client == null ? "—" : a.Client.Prenom + " " + a.Client.Nom,
                DateCreation = a.DateCreation
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> AvisExisteAsync(Guid commandeId, CancellationToken ct = default) =>
        _db.Avis.AnyAsync(a => a.CommandeId == commandeId, ct);
}
