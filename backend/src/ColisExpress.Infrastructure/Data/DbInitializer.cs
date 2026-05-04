using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColisExpress.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColisExpressDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync(ct);

        // Backfill coordonnées GPS sur points relais existants (idempotent)
        await BackfillCoordonneesAsync(db, ct);

        // Données de démo massives pour tests (idempotent, marqueur "[BIGSEED-V1]")
        await GenerateMassiveTestDataAsync(db, ct);

        if (await db.Utilisateurs.AnyAsync(ct)) return;

        var pwd = hasher.Hash("Test1234!");

        // ADMIN
        db.Utilisateurs.Add(new Utilisateur
        {
            Role = RoleUtilisateur.Admin, Prenom = "Super", Nom = "Admin",
            Email = "admin@test.com", Telephone = "+33 1 00 00 00 00",
            MotDePasseHash = hasher.Hash("Admin123!"),
            StatutCompte = StatutCompte.Actif, EmailVerifie = true
        });

        // 5 CLIENTS
        for (int i = 1; i <= 5; i++)
            db.Utilisateurs.Add(new Utilisateur
            {
                Role = RoleUtilisateur.Client, Prenom = $"Client{i}", Nom = $"Test{i}",
                Email = $"client{i}@test.com", Telephone = $"+33 6 00 00 00 0{i}",
                MotDePasseHash = pwd, StatutCompte = StatutCompte.Actif, EmailVerifie = true
            });
        await db.SaveChangesAsync(ct);

        // 5 TRANSPORTEURS
        var tNames = new[] { ("Karim","Benali"), ("Sofia","Moreau"), ("Yanis","Cherifi"), ("Amina","Larbi"), ("Mehdi","Bouzid") };
        var vehic = new[] { "Fourgon 12m³", "Fourgon 8m³", "Camion 20m³", "Utilitaire 6m³", "Camion 30m³+" };
        var corr = new[] { "FR-DZ", "FR-MA", "FR-TN", "FR-DZ,FR-MA", "FR-FR,FR-DZ" };
        var notes = new[] { 4.9m, 4.7m, 4.8m, 4.6m, 4.5m };
        var nbAvis = new[] { 127, 83, 205, 42, 15 };
        var transporteurs = new List<Transporteur>();

        for (int i = 0; i < 5; i++)
        {
            var u = new Utilisateur
            {
                Role = RoleUtilisateur.Transporteur, Prenom = tNames[i].Item1, Nom = tNames[i].Item2,
                Email = $"transporteur{i + 1}@test.com", Telephone = $"+33 6 10 00 00 0{i + 1}",
                MotDePasseHash = pwd, StatutCompte = StatutCompte.Actif, EmailVerifie = true
            };
            db.Utilisateurs.Add(u);
            await db.SaveChangesAsync(ct);
            var t = new Transporteur
            {
                UtilisateurId = u.Id, StatutKyc = StatutKyc.Valide,
                NoteMoyenne = notes[i], NombreAvis = nbAvis[i],
                TypeVehicule = vehic[i], CorridorsActifs = corr[i]
            };
            db.Transporteurs.Add(t);
            transporteurs.Add(t);
        }
        await db.SaveChangesAsync(ct);

        // 10 POINTS RELAIS
        var rData = new (string Nom, string Adr, string Ville, string Dept, string Region, string Pays, string Tel, string Jours, string HO, string HF, string? HOW, string? HFW, double Lat, double Lng)[]
        {
            ("Relais Paris 15e", "42 rue de la Convention", "Paris", "Paris", "Île-de-France", "France", "+33 1 45 67 89 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "19:00", "09:00", "14:00", 48.8417, 2.2916),
            ("Relais Lyon Part-Dieu", "15 cours Lafayette", "Lyon", "Rhône", "Auvergne-Rhône-Alpes", "France", "+33 4 72 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:30", "18:30", "09:00", "13:00", 45.7605, 4.8590),
            ("Relais Marseille Vieux-Port", "8 quai du Port", "Marseille", "Bouches-du-Rhône", "Provence-Alpes-Côte d'Azur", "France", "+33 4 91 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "18:00", "09:00", "13:00", 43.2965, 5.3683),
            ("Relais Toulouse Capitole", "25 rue Alsace-Lorraine", "Toulouse", "Haute-Garonne", "Occitanie", "France", "+33 5 61 00 00 00", "Lun,Mar,Mer,Jeu,Ven", "09:00", "18:00", null, null, 43.6047, 1.4442),
            ("Relais Lille Flandres", "3 place de la Gare", "Lille", "Nord", "Hauts-de-France", "France", "+33 3 20 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "19:00", "09:00", "14:00", 50.6365, 3.0707),
            ("Relais Alger Hydra", "12 rue des Frères Bouadou", "Alger", "Alger", "Alger", "Algérie", "+213 21 60 00 00", "Dim,Lun,Mar,Mer,Jeu", "08:00", "17:00", null, null, 36.7437, 3.0431),
            ("Relais Oran Centre", "45 boulevard de la Soummam", "Oran", "Oran", "Oran", "Algérie", "+213 41 40 00 00", "Dim,Lun,Mar,Mer,Jeu", "08:30", "17:00", null, null, 35.6976, -0.6337),
            ("Relais Casablanca Ain Diab", "45 boulevard de la Corniche", "Casablanca", "Casablanca-Settat", "Casablanca-Settat", "Maroc", "+212 522 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "18:00", "09:00", "13:00", 33.5731, -7.5898),
            ("Relais Rabat Agdal", "22 avenue de France", "Rabat", "Rabat-Salé-Kénitra", "Rabat-Salé-Kénitra", "Maroc", "+212 537 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "18:00", "09:00", "13:00", 34.0209, -6.8416),
            ("Relais Tunis Centre", "15 avenue Habib Bourguiba", "Tunis", "Tunis", "Tunis", "Tunisie", "+216 71 00 00 00", "Lun,Mar,Mer,Jeu,Ven,Sam", "08:00", "17:30", "09:00", "13:00", 36.8008, 10.1815),
        };

        for (int i = 0; i < rData.Length; i++)
        {
            var r = rData[i];
            var u = new Utilisateur
            {
                Role = RoleUtilisateur.PointRelais, Prenom = "Relais", Nom = r.Nom,
                Email = $"relais{i + 1}@test.com", Telephone = r.Tel,
                MotDePasseHash = pwd, StatutCompte = StatutCompte.Actif, EmailVerifie = true
            };
            db.Utilisateurs.Add(u);
            await db.SaveChangesAsync(ct);
            db.PointsRelais.Add(new PointRelais
            {
                UtilisateurId = u.Id, NomRelais = r.Nom, Adresse = r.Adr,
                Ville = r.Ville, Departement = r.Dept, Region = r.Region, Pays = r.Pays,
                Telephone = r.Tel, EstActif = true, JoursOuverture = r.Jours,
                HeureOuverture = TimeOnly.Parse(r.HO), HeureFermeture = TimeOnly.Parse(r.HF),
                HeureOuvertureWeekend = r.HOW != null ? TimeOnly.Parse(r.HOW) : null,
                HeureFermetureWeekend = r.HFW != null ? TimeOnly.Parse(r.HFW) : null,
                Latitude = r.Lat, Longitude = r.Lng,
            });
        }
        await db.SaveChangesAsync(ct);

        // TRAJETS
        var now = DateTime.UtcNow;
        db.Trajets.AddRange(
            new Trajet { TransporteurId = transporteurs[0].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "Algérie", VilleArrivee = "Alger", DateDepart = now.AddDays(4), DateEstimeeArrivee = now.AddDays(7), CapaciteMaxPoids = 500, NombreMaxColis = 30, CapaciteRestante = 30, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 85m, SupplementUrgent = 15m, SupplementFragile = 10m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[0].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "Algérie", VilleArrivee = "Oran", DateDepart = now.AddDays(8), DateEstimeeArrivee = now.AddDays(12), CapaciteMaxPoids = 500, NombreMaxColis = 30, CapaciteRestante = 30, ModeTarification = ModeTarification.PrixAuKilo, PrixAuKilo = 7m, SupplementUrgent = 20m, SupplementFragile = 12m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[1].Id, PaysDepart = "France", VilleDepart = "Lyon", PaysArrivee = "Maroc", VilleArrivee = "Casablanca", DateDepart = now.AddDays(5), DateEstimeeArrivee = now.AddDays(9), CapaciteMaxPoids = 300, NombreMaxColis = 20, CapaciteRestante = 20, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 72m, SupplementUrgent = 18m, SupplementFragile = 8m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[1].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "Maroc", VilleArrivee = "Casablanca", DateDepart = now.AddDays(6), DateEstimeeArrivee = now.AddDays(10), CapaciteMaxPoids = 300, NombreMaxColis = 20, CapaciteRestante = 20, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 78m, SupplementUrgent = 18m, SupplementFragile = 8m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[2].Id, PaysDepart = "France", VilleDepart = "Marseille", PaysArrivee = "Tunisie", VilleArrivee = "Tunis", DateDepart = now.AddDays(3), DateEstimeeArrivee = now.AddDays(6), CapaciteMaxPoids = 800, NombreMaxColis = 50, CapaciteRestante = 50, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 65m, SupplementUrgent = 15m, SupplementFragile = 10m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[2].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "Algérie", VilleArrivee = "Alger", DateDepart = now.AddDays(10), DateEstimeeArrivee = now.AddDays(13), CapaciteMaxPoids = 800, NombreMaxColis = 50, CapaciteRestante = 50, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 95m, SupplementUrgent = 20m, SupplementFragile = 12m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[3].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "France", VilleArrivee = "Lyon", DateDepart = now.AddDays(2), DateEstimeeArrivee = now.AddDays(2), CapaciteMaxPoids = 200, NombreMaxColis = 15, CapaciteRestante = 15, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 35m, SupplementUrgent = 10m, SupplementFragile = 5m, Statut = StatutTrajet.Actif },
            new Trajet { TransporteurId = transporteurs[4].Id, PaysDepart = "France", VilleDepart = "Paris", PaysArrivee = "France", VilleArrivee = "Marseille", DateDepart = now.AddDays(3), DateEstimeeArrivee = now.AddDays(3), CapaciteMaxPoids = 400, NombreMaxColis = 25, CapaciteRestante = 25, ModeTarification = ModeTarification.PrixParColis, PrixParColis = 45m, SupplementUrgent = 12m, SupplementFragile = 8m, Statut = StatutTrajet.Actif }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task BackfillCoordonneesAsync(ColisExpressDbContext db, CancellationToken ct)
    {
        var coords = new Dictionary<string, (double Lat, double Lng)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Paris"] = (48.8566, 2.3522),
            ["Lyon"] = (45.7640, 4.8357),
            ["Marseille"] = (43.2965, 5.3698),
            ["Toulouse"] = (43.6047, 1.4442),
            ["Lille"] = (50.6292, 3.0573),
            ["Bordeaux"] = (44.8378, -0.5792),
            ["Nice"] = (43.7102, 7.2620),
            ["Strasbourg"] = (48.5734, 7.7521),
            ["Nantes"] = (47.2184, -1.5536),
            ["Alger"] = (36.7538, 3.0588),
            ["Oran"] = (35.6976, -0.6337),
            ["Constantine"] = (36.3650, 6.6147),
            ["Annaba"] = (36.9000, 7.7667),
            ["Casablanca"] = (33.5731, -7.5898),
            ["Rabat"] = (34.0209, -6.8416),
            ["Marrakech"] = (31.6295, -7.9811),
            ["Tunis"] = (36.8065, 10.1815),
            ["Sfax"] = (34.7406, 10.7603),
        };

        var relais = await db.PointsRelais
            .Where(r => r.Latitude == null || r.Longitude == null)
            .ToListAsync(ct);

        if (relais.Count == 0) return;

        var changed = false;
        foreach (var r in relais)
        {
            if (coords.TryGetValue(r.Ville, out var c))
            {
                r.Latitude = c.Lat;
                r.Longitude = c.Lng;
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    /// Génère un gros volume de données de test couvrant 4 semaines passées + 12 semaines futures.
    /// Trajets hebdomadaires Paris → Alger via Lyon et Marseille pour transporteur1, avec 4 commandes
    /// client1 par trajet (segments variés, statuts variés selon la date pour avoir tous les cas de test).
    /// Idempotent : marqueur "[BIGSEED-V1]" dans Trajet.Conditions.
    private static async Task GenerateMassiveTestDataAsync(ColisExpressDbContext db, CancellationToken ct)
    {
        const string MARKER = "[BIGSEED-V2]";
        // Cleanup éventuel de la V1 (chèques, etc.)
        await CleanupOldSeedAsync(db, "[BIGSEED-V1]", ct);
        if (await db.Trajets.AnyAsync(t => t.Conditions == MARKER, ct)) return;

        var transporteurUser = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == "transporteur1@test.com", ct);
        var clientUser = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == "client1@test.com", ct);
        if (transporteurUser is null || clientUser is null) return;

        var transporteur = await db.Transporteurs.FirstOrDefaultAsync(t => t.UtilisateurId == transporteurUser.Id, ct);
        if (transporteur is null) return;

        // Récupération des points relais nécessaires (par ville)
        var relaisParis = await db.PointsRelais.FirstOrDefaultAsync(r => r.Ville == "Paris", ct);
        var relaisLyon = await db.PointsRelais.FirstOrDefaultAsync(r => r.Ville == "Lyon", ct);
        var relaisMarseille = await db.PointsRelais.FirstOrDefaultAsync(r => r.Ville == "Marseille", ct);
        var relaisAlger = await db.PointsRelais.FirstOrDefaultAsync(r => r.Ville == "Alger", ct);
        if (relaisParis is null || relaisLyon is null || relaisMarseille is null || relaisAlger is null) return;

        // Lundi de la semaine en cours (UTC)
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var thisMonday = DateTime.SpecifyKind(today.AddDays(-daysSinceMonday), DateTimeKind.Utc);

        // Tarifs par segment
        var prixSegment = new Dictionary<(string, string), decimal>
        {
            [("Paris", "Alger")] = 85m,
            [("Paris", "Lyon")] = 25m,
            [("Paris", "Marseille")] = 40m,
            [("Lyon", "Marseille")] = 20m,
            [("Lyon", "Alger")] = 65m,
            [("Marseille", "Alger")] = 55m,
        };

        var rng = new Random(42);
        var anneeCode = DateTime.UtcNow.Year;
        var compteurColis = await db.Colis.CountAsync(ct) + 100;

        // Génération de -4 semaines (passé) à +12 semaines (3 mois futur) = 17 trajets
        for (int w = -4; w <= 12; w++)
        {
            var lundiDepart = thisMonday.AddDays(w * 7).AddHours(7); // 7h UTC
            var vendrediArrivee = thisMonday.AddDays(w * 7 + 4).AddHours(18); // vendredi 18h UTC

            var trajet = new Trajet
            {
                TransporteurId = transporteur.Id,
                PaysDepart = "France",
                VilleDepart = "Paris",
                PaysArrivee = "Algérie",
                VilleArrivee = "Alger",
                DateDepart = lundiDepart,
                DateEstimeeArrivee = vendrediArrivee,
                CapaciteMaxPoids = 500,
                NombreMaxColis = 30,
                CapaciteRestante = 26, // 4 colis seront réservés
                ModeTarification = ModeTarification.PrixParColis,
                PrixParColis = 85m,
                SupplementUrgent = 15m,
                SupplementFragile = 10m,
                Statut = w < -1 ? StatutTrajet.Termine : StatutTrajet.Actif,
                PointDepot = "Relais Paris 15e",
                Conditions = MARKER,
                DateCreation = lundiDepart.AddDays(-14)
            };
            db.Trajets.Add(trajet);
            await db.SaveChangesAsync(ct);

            // Étapes : Lyon (mardi) puis Marseille (mercredi)
            db.EtapesTrajets.AddRange(
                new EtapeTrajet
                {
                    TrajetId = trajet.Id, PointRelaisId = relaisLyon.Id, Ordre = 1,
                    HeureEstimeeArrivee = lundiDepart.AddDays(1).AddHours(5), // mardi midi
                    RelaisOuvertALArrivee = true,
                    Statut = w < 0 ? StatutEtape.Terminee : StatutEtape.Planifiee
                },
                new EtapeTrajet
                {
                    TrajetId = trajet.Id, PointRelaisId = relaisMarseille.Id, Ordre = 2,
                    HeureEstimeeArrivee = lundiDepart.AddDays(2).AddHours(7), // mercredi 14h
                    RelaisOuvertALArrivee = true,
                    Statut = w < 0 ? StatutEtape.Terminee : StatutEtape.Planifiee
                }
            );
            await db.SaveChangesAsync(ct);

            // 4 commandes client1 par trajet, segments variés
            var segments = new[]
            {
                ("Paris", "Alger", relaisParis.Id, relaisAlger.Id, "Alger"),
                ("Paris", "Lyon", relaisParis.Id, relaisLyon.Id, "Lyon"),
                ("Paris", "Marseille", relaisParis.Id, relaisMarseille.Id, "Marseille"),
                ("Lyon", "Marseille", relaisLyon.Id, relaisMarseille.Id, "Marseille"),
            };

            for (int s = 0; s < segments.Length; s++)
            {
                var (depart, arrivee, relaisDep, relaisArr, villeDest) = segments[s];
                var prix = prixSegment[(depart, arrivee)];
                var poids = 2m + (decimal)rng.NextDouble() * 8m;
                poids = Math.Round(poids, 1);

                // Mode de règlement : Carte (60%) ou Espèces (40%) — pas de chèque
                var mode = rng.Next(100) < 60 ? ModeReglement.Carte : ModeReglement.Especes;

                // Statut + paiement déterminés par la position du trajet dans le temps
                var (statutColis, statutPaiement) = DeriveStatuts(w, s, mode, rng);

                compteurColis++;
                var codeColis = $"COL-{anneeCode}-{compteurColis:D4}";

                var commande = new Commande
                {
                    ClientId = clientUser.Id,
                    TransporteurId = transporteur.Id,
                    TrajetId = trajet.Id,
                    RelaisDepartId = relaisDep,
                    RelaisArriveeId = relaisArr,
                    SegmentDepart = depart,
                    SegmentArrivee = arrivee,
                    NomDestinataire = ChoisirDestinataire(s, w),
                    TelephoneDestinataire = $"+213 5{rng.Next(10, 99)}{rng.Next(100000, 999999)}",
                    VilleDestinataire = villeDest,
                    DescriptionContenu = ChoisirContenu(s),
                    PoidsDeclare = poids,
                    Dimensions = $"{rng.Next(20, 50)}x{rng.Next(20, 40)}x{rng.Next(15, 35)}",
                    ValeurDeclaree = (decimal)rng.Next(50, 500),
                    PrixTransport = prix,
                    FraisService = Math.Round(prix * 0.05m, 2),
                    SupplementsTotal = 0m,
                    Total = prix + Math.Round(prix * 0.05m, 2),
                    ModeReglement = mode,
                    StatutReglement = statutPaiement,
                    DateCreation = lundiDepart.AddDays(-7 - rng.Next(1, 5))
                };
                db.Commandes.Add(commande);
                await db.SaveChangesAsync(ct);

                var colis = new Colis
                {
                    CommandeId = commande.Id,
                    CodeColis = codeColis,
                    QrCodeData = codeColis,
                    CodeRetrait = rng.Next(1000, 9999).ToString(),
                    Statut = statutColis,
                    DateCreation = commande.DateCreation
                };
                db.Colis.Add(colis);
                await db.SaveChangesAsync(ct);

                // Événements selon le statut atteint
                AjouterEvenements(db, colis, lundiDepart, statutColis, transporteurUser.Id, clientUser.Id);

                // Si paiement en espèces déjà payé : créer le Paiement avec RelaisEncaisseurId
                if (mode == ModeReglement.Especes && statutPaiement == StatutReglement.Paye)
                {
                    db.Paiements.Add(new Paiement
                    {
                        CommandeId = commande.Id,
                        Mode = ModeReglement.Especes,
                        Montant = commande.Total,
                        Statut = StatutReglement.Paye,
                        DateEncaissement = commande.DateCreation.AddDays(rng.Next(1, 4)),
                        ReferenceExterne = "Espèces encaissées par relais départ",
                        RelaisEncaisseurId = relaisDep,
                        EstReverseAdmin = w < -2, // les vieux encaissements sont déjà reversés
                        DateReversement = w < -2 ? commande.DateCreation.AddDays(7) : null
                    });
                }
                else if (mode == ModeReglement.Carte && statutPaiement == StatutReglement.Paye)
                {
                    db.Paiements.Add(new Paiement
                    {
                        CommandeId = commande.Id,
                        Mode = ModeReglement.Carte,
                        Montant = commande.Total,
                        Statut = StatutReglement.Paye,
                        DateEncaissement = commande.DateCreation,
                        ReferenceExterne = $"cs_test_{Guid.NewGuid():N}"
                    });
                }
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task CleanupOldSeedAsync(ColisExpressDbContext db, string oldMarker, CancellationToken ct)
    {
        var trajets = await db.Trajets.Where(t => t.Conditions == oldMarker).Select(t => t.Id).ToListAsync(ct);
        if (trajets.Count == 0) return;

        var commandes = await db.Commandes.Where(c => trajets.Contains(c.TrajetId)).Select(c => c.Id).ToListAsync(ct);
        var colisIds = await db.Colis.Where(c => commandes.Contains(c.CommandeId)).Select(c => c.Id).ToListAsync(ct);

        await db.EvenementsColis.Where(e => colisIds.Contains(e.ColisId)).ExecuteDeleteAsync(ct);
        await db.Paiements.Where(p => commandes.Contains(p.CommandeId)).ExecuteDeleteAsync(ct);
        await db.Colis.Where(c => commandes.Contains(c.CommandeId)).ExecuteDeleteAsync(ct);
        await db.Commandes.Where(c => trajets.Contains(c.TrajetId)).ExecuteDeleteAsync(ct);
        await db.EtapesTrajets.Where(e => trajets.Contains(e.TrajetId)).ExecuteDeleteAsync(ct);
        await db.Trajets.Where(t => t.Conditions == oldMarker).ExecuteDeleteAsync(ct);
    }

    private static (StatutColis statut, StatutReglement paiement) DeriveStatuts(int weekOffset, int segmentIndex, ModeReglement mode, Random rng)
    {
        // Trajets passés (>1 semaine en arrière) : tous livrés et payés
        if (weekOffset < -1)
            return (StatutColis.LivraisonCloturee, StatutReglement.Paye);

        // Semaine dernière : mix livrés / disponibles au retrait
        if (weekOffset == -1)
        {
            return segmentIndex switch
            {
                0 => (StatutColis.LivraisonCloturee, StatutReglement.Paye),
                1 => (StatutColis.DisponibleAuRetrait, StatutReglement.Paye),
                2 => (StatutColis.RetireParDestinataire, StatutReglement.Paye),
                _ => (StatutColis.LivraisonCloturee, StatutReglement.Paye),
            };
        }

        // Semaine en cours : en transit / réceptionné / déposé
        if (weekOffset == 0)
        {
            return segmentIndex switch
            {
                0 => (StatutColis.EnTransit, StatutReglement.Paye),
                1 => (StatutColis.ReceptionneParTransporteur, StatutReglement.Paye),
                2 => (StatutColis.DeposeParClient, StatutReglement.Paye),
                _ => (StatutColis.ArriveDansPaysDest, StatutReglement.Paye),
            };
        }

        // Semaine prochaine : déposé / en attente dépôt
        if (weekOffset == 1)
        {
            return segmentIndex switch
            {
                0 => (StatutColis.DeposeParClient, StatutReglement.Paye),
                1 => (StatutColis.EnAttenteDepot, StatutReglement.Paye),
                2 => (StatutColis.ReservationConfirmee, StatutReglement.Paye),
                _ => (StatutColis.EnAttenteReglement, mode == ModeReglement.Especes ? StatutReglement.EnAttente : StatutReglement.Paye),
            };
        }

        // Trajets futurs : réservation confirmée + quelques en attente règlement
        if (segmentIndex == 3 && mode == ModeReglement.Especes)
            return (StatutColis.EnAttenteReglement, StatutReglement.EnAttente);

        if (mode == ModeReglement.Carte)
            return (StatutColis.ReservationConfirmee, StatutReglement.Paye);

        // Espèces sur trajets futurs : paiement non encore effectué
        return (StatutColis.EnAttenteReglement, StatutReglement.EnAttente);
    }

    private static void AjouterEvenements(ColisExpressDbContext db, Colis colis, DateTime lundiDepart, StatutColis cible, Guid transporteurUserId, Guid clientUserId)
    {
        var events = new List<EvenementColis>();
        var dateCreation = colis.DateCreation;

        // Toujours : DemandeCreee
        events.Add(new EvenementColis
        {
            ColisId = colis.Id, AncienStatut = StatutColis.Brouillon, NouveauStatut = StatutColis.DemandeCreee,
            ActeurId = clientUserId, DateHeure = dateCreation, Commentaire = "Commande créée par le client"
        });

        // Statuts intermédiaires selon la cible
        var enchainement = new[]
        {
            StatutColis.ReservationConfirmee,
            StatutColis.CodeColisGenere,
            StatutColis.EnAttenteDepot,
            StatutColis.DeposeParClient,
            StatutColis.ReceptionneParTransporteur,
            StatutColis.EnTransit,
            StatutColis.ArriveDansPaysDest,
            StatutColis.DisponibleAuRetrait,
            StatutColis.RetireParDestinataire,
            StatutColis.LivraisonCloturee
        };

        var dateCourante = dateCreation;
        var precedent = StatutColis.DemandeCreee;
        foreach (var step in enchainement)
        {
            if (step > cible) break;

            // Calcul d'une date plausible
            dateCourante = step switch
            {
                StatutColis.ReservationConfirmee => dateCreation.AddHours(1),
                StatutColis.CodeColisGenere => dateCreation.AddHours(1).AddMinutes(5),
                StatutColis.EnAttenteDepot => dateCreation.AddDays(1),
                StatutColis.DeposeParClient => lundiDepart.AddHours(-2),
                StatutColis.ReceptionneParTransporteur => lundiDepart,
                StatutColis.EnTransit => lundiDepart.AddHours(2),
                StatutColis.ArriveDansPaysDest => lundiDepart.AddDays(4),
                StatutColis.DisponibleAuRetrait => lundiDepart.AddDays(4).AddHours(2),
                StatutColis.RetireParDestinataire => lundiDepart.AddDays(5),
                StatutColis.LivraisonCloturee => lundiDepart.AddDays(5).AddMinutes(1),
                _ => dateCourante
            };

            var acteur = step is StatutColis.DeposeParClient or StatutColis.DisponibleAuRetrait or StatutColis.RetireParDestinataire or StatutColis.LivraisonCloturee
                ? clientUserId : transporteurUserId;

            events.Add(new EvenementColis
            {
                ColisId = colis.Id, AncienStatut = precedent, NouveauStatut = step,
                ActeurId = acteur, DateHeure = dateCourante,
                Commentaire = LibelleEvenement(step)
            });
            precedent = step;

            if (step == cible) break;
        }

        db.EvenementsColis.AddRange(events);
    }

    private static string LibelleEvenement(StatutColis s) => s switch
    {
        StatutColis.ReservationConfirmee => "Réservation confirmée par le transporteur",
        StatutColis.CodeColisGenere => "Code colis et QR code générés",
        StatutColis.EnAttenteDepot => "En attente du dépôt par le client",
        StatutColis.DeposeParClient => "Colis déposé par le client au point relais départ",
        StatutColis.ReceptionneParTransporteur => "Colis réceptionné par le transporteur",
        StatutColis.EnTransit => "Colis en transit",
        StatutColis.ArriveDansPaysDest => "Colis arrivé dans le pays de destination",
        StatutColis.DisponibleAuRetrait => "Colis disponible au retrait au point relais",
        StatutColis.RetireParDestinataire => "Colis retiré par le destinataire (code vérifié)",
        StatutColis.LivraisonCloturee => "Livraison clôturée",
        _ => s.ToString()
    };

    private static string ChoisirDestinataire(int segmentIndex, int weekOffset)
    {
        var noms = new[]
        {
            "Karim Boudiaf", "Yasmine Belkacem", "Mohamed Hadj", "Leila Saadi",
            "Amine Cherif", "Nadia Bensalem", "Rachid Mansouri", "Samira Khelifi",
            "Hocine Belaid", "Farida Touati"
        };
        var idx = Math.Abs((segmentIndex * 7 + weekOffset * 3) % noms.Length);
        return noms[idx];
    }

    private static string ChoisirContenu(int segmentIndex) => segmentIndex switch
    {
        0 => "Vêtements, produits cosmétiques, médicaments sans ordonnance",
        1 => "Documents administratifs, livres",
        2 => "Petit électroménager, accessoires",
        _ => "Cadeaux famille, vêtements enfants",
    };
}
