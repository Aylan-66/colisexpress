using ColisExpress.Application.Interfaces;
using ColisExpress.Domain.Entities;
using ColisExpress.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ColisExpress.Infrastructure.Data;

public static class DbInitializer
{
    public const string AdminEmail = "admin@colisexpress.fr";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColisExpressDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync(ct);

        if (!await db.Utilisateurs.AnyAsync(u => u.Role == RoleUtilisateur.Admin, ct))
        {
            db.Utilisateurs.Add(new Utilisateur
            {
                Role = RoleUtilisateur.Admin,
                Prenom = "Super",
                Nom = "Admin",
                Email = AdminEmail,
                Telephone = "+33 1 00 00 00 00",
                MotDePasseHash = hasher.Hash(AdminPassword),
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Transporteurs.AnyAsync(ct))
        {
            var pwd = hasher.Hash("Test1234!");

            var userKarim = new Utilisateur
            {
                Role = RoleUtilisateur.Transporteur,
                Prenom = "Karim",
                Nom = "Benali",
                Email = "karim.benali@example.fr",
                Telephone = "+33 6 12 34 56 78",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            };
            var userSofia = new Utilisateur
            {
                Role = RoleUtilisateur.Transporteur,
                Prenom = "Sofia",
                Nom = "Moreau",
                Email = "sofia.moreau@example.fr",
                Telephone = "+33 6 98 76 54 32",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            };
            var userYanis = new Utilisateur
            {
                Role = RoleUtilisateur.Transporteur,
                Prenom = "Yanis",
                Nom = "Cherifi",
                Email = "yanis.cherifi@example.fr",
                Telephone = "+33 6 55 44 33 22",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            };
            var userPending = new Utilisateur
            {
                Role = RoleUtilisateur.Transporteur,
                Prenom = "Mehdi",
                Nom = "Larbi",
                Email = "mehdi.larbi@example.fr",
                Telephone = "+33 6 11 22 33 44",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = false
            };
            db.Utilisateurs.AddRange(userKarim, userSofia, userYanis, userPending);
            await db.SaveChangesAsync(ct);

            var transKarim = new Transporteur
            {
                UtilisateurId = userKarim.Id,
                StatutKyc = StatutKyc.Valide,
                NoteMoyenne = 4.9m,
                NombreAvis = 127,
                TypeVehicule = "Fourgon 12m³",
                CorridorsActifs = "FR-DZ"
            };
            var transSofia = new Transporteur
            {
                UtilisateurId = userSofia.Id,
                StatutKyc = StatutKyc.Valide,
                NoteMoyenne = 4.7m,
                NombreAvis = 83,
                TypeVehicule = "Fourgon 8m³",
                CorridorsActifs = "FR-MA"
            };
            var transYanis = new Transporteur
            {
                UtilisateurId = userYanis.Id,
                StatutKyc = StatutKyc.Valide,
                NoteMoyenne = 4.8m,
                NombreAvis = 205,
                TypeVehicule = "Camion 20m³",
                CorridorsActifs = "FR-DZ,FR-TN"
            };
            var transPending = new Transporteur
            {
                UtilisateurId = userPending.Id,
                StatutKyc = StatutKyc.EnAttente,
                NoteMoyenne = 0,
                NombreAvis = 0,
                TypeVehicule = "Utilitaire 6m³",
                CorridorsActifs = "FR-MA"
            };
            db.Transporteurs.AddRange(transKarim, transSofia, transYanis, transPending);
            await db.SaveChangesAsync(ct);

            var now = DateTime.UtcNow;
            db.Trajets.AddRange(
                new Trajet {
                    TransporteurId = transKarim.Id,
                    PaysDepart = "France", VilleDepart = "Paris",
                    PaysArrivee = "Algérie", VilleArrivee = "Alger",
                    DateDepart = now.AddDays(4), DateEstimeeArrivee = now.AddDays(7),
                    CapaciteMaxPoids = 500, NombreMaxColis = 30, CapaciteRestante = 28,
                    ModeTarification = ModeTarification.PrixParColis, PrixParColis = 85m,
                    SupplementUrgent = 15m, SupplementFragile = 10m,
                    PointDepot = "Paris 15e — 42 rue de la Convention",
                    Statut = StatutTrajet.Actif
                },
                new Trajet {
                    TransporteurId = transKarim.Id,
                    PaysDepart = "France", VilleDepart = "Paris",
                    PaysArrivee = "Algérie", VilleArrivee = "Oran",
                    DateDepart = now.AddDays(8), DateEstimeeArrivee = now.AddDays(12),
                    CapaciteMaxPoids = 500, NombreMaxColis = 30, CapaciteRestante = 30,
                    ModeTarification = ModeTarification.PrixAuKilo, PrixAuKilo = 7m,
                    SupplementUrgent = 20m, SupplementFragile = 12m,
                    PointDepot = "Paris 15e — 42 rue de la Convention",
                    Statut = StatutTrajet.Actif
                },
                new Trajet {
                    TransporteurId = transSofia.Id,
                    PaysDepart = "France", VilleDepart = "Lyon",
                    PaysArrivee = "Maroc", VilleArrivee = "Casablanca",
                    DateDepart = now.AddDays(5), DateEstimeeArrivee = now.AddDays(9),
                    CapaciteMaxPoids = 300, NombreMaxColis = 20, CapaciteRestante = 17,
                    ModeTarification = ModeTarification.PrixParColis, PrixParColis = 72m,
                    SupplementUrgent = 18m, SupplementFragile = 8m,
                    PointDepot = "Lyon 3e — 15 cours Lafayette",
                    Statut = StatutTrajet.Actif
                },
                new Trajet {
                    TransporteurId = transSofia.Id,
                    PaysDepart = "France", VilleDepart = "Paris",
                    PaysArrivee = "Maroc", VilleArrivee = "Casablanca",
                    DateDepart = now.AddDays(6), DateEstimeeArrivee = now.AddDays(10),
                    CapaciteMaxPoids = 300, NombreMaxColis = 20, CapaciteRestante = 20,
                    ModeTarification = ModeTarification.PrixParColis, PrixParColis = 78m,
                    SupplementUrgent = 18m, SupplementFragile = 8m,
                    PointDepot = "Paris 18e — 8 rue Ordener",
                    Statut = StatutTrajet.Actif
                },
                new Trajet {
                    TransporteurId = transYanis.Id,
                    PaysDepart = "France", VilleDepart = "Marseille",
                    PaysArrivee = "Tunisie", VilleArrivee = "Tunis",
                    DateDepart = now.AddDays(3), DateEstimeeArrivee = now.AddDays(6),
                    CapaciteMaxPoids = 800, NombreMaxColis = 50, CapaciteRestante = 45,
                    ModeTarification = ModeTarification.PrixParColis, PrixParColis = 65m,
                    SupplementUrgent = 15m, SupplementFragile = 10m,
                    PointDepot = "Marseille 2e — Port de Marseille",
                    Statut = StatutTrajet.Actif
                },
                new Trajet {
                    TransporteurId = transYanis.Id,
                    PaysDepart = "France", VilleDepart = "Paris",
                    PaysArrivee = "Algérie", VilleArrivee = "Alger",
                    DateDepart = now.AddDays(10), DateEstimeeArrivee = now.AddDays(13),
                    CapaciteMaxPoids = 800, NombreMaxColis = 50, CapaciteRestante = 48,
                    ModeTarification = ModeTarification.PrixParColis, PrixParColis = 95m,
                    SupplementUrgent = 20m, SupplementFragile = 12m,
                    PointDepot = "Paris 19e — 12 rue Petit",
                    Statut = StatutTrajet.Actif
                }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PointsRelais.AnyAsync(ct))
        {
            var pwd = hasher.Hash("Relais1234!");

            var userRelaisAlger = new Utilisateur
            {
                Role = RoleUtilisateur.PointRelais,
                Prenom = "Relais",
                Nom = "Alger Centre",
                Email = "alger.centre@colisexpress.fr",
                Telephone = "+213 21 00 00 00",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            };
            var userRelaisCasa = new Utilisateur
            {
                Role = RoleUtilisateur.PointRelais,
                Prenom = "Relais",
                Nom = "Casablanca Ain",
                Email = "casablanca.ain@colisexpress.fr",
                Telephone = "+212 522 00 00 00",
                MotDePasseHash = pwd,
                StatutCompte = StatutCompte.Actif,
                EmailVerifie = true
            };
            db.Utilisateurs.AddRange(userRelaisAlger, userRelaisCasa);
            await db.SaveChangesAsync(ct);

            db.PointsRelais.AddRange(
                new PointRelais
                {
                    UtilisateurId = userRelaisAlger.Id,
                    NomRelais = "Relais Hydra",
                    Adresse = "12 rue des Frères Bouadou",
                    Ville = "Alger",
                    Pays = "Algérie",
                    Telephone = "+213 21 00 00 00",
                    EstActif = true
                },
                new PointRelais
                {
                    UtilisateurId = userRelaisCasa.Id,
                    NomRelais = "Relais Ain Diab",
                    Adresse = "45 boulevard de la Corniche",
                    Ville = "Casablanca",
                    Pays = "Maroc",
                    Telephone = "+212 522 00 00 00",
                    EstActif = true
                }
            );
            await db.SaveChangesAsync(ct);
        }
    }
}
