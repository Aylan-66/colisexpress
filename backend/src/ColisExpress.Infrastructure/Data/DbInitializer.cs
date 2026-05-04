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
}
