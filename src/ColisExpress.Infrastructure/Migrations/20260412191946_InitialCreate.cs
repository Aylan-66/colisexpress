using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "utilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Telephone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatutCompte = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmailVerifie = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "points_relais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomRelais = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ville = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Pays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telephone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_points_relais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_points_relais_utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transporteurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatutKyc = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NoteMoyenne = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    NombreAvis = table.Column<int>(type: "integer", nullable: false),
                    TypeVehicule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorridorsActifs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transporteurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transporteurs_utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents_kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeDocument = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NomFichier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CheminFichier = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Statut = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DateSoumission = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidePar = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents_kyc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_kyc_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trajets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaysDepart = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VilleDepart = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PaysArrivee = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VilleArrivee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DateDepart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateEstimeeArrivee = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CapaciteMaxPoids = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NombreMaxColis = table.Column<int>(type: "integer", nullable: false),
                    CapaciteRestante = table.Column<int>(type: "integer", nullable: false),
                    ModeTarification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PrixParColis = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PrixAuKilo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SupplementUrgent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SupplementFragile = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PointDepot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Conditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Statut = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trajets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trajets_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commandes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrajetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelaisDepartId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelaisArriveeId = table.Column<Guid>(type: "uuid", nullable: true),
                    NomDestinataire = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TelephoneDestinataire = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VilleDestinataire = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DescriptionContenu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PoidsDeclare = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Dimensions = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValeurDeclaree = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PrixTransport = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    FraisService = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SupplementsTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ModeReglement = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StatutReglement = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InstructionsParticulieres = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commandes_points_relais_RelaisArriveeId",
                        column: x => x.RelaisArriveeId,
                        principalTable: "points_relais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_commandes_points_relais_RelaisDepartId",
                        column: x => x.RelaisDepartId,
                        principalTable: "points_relais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_commandes_trajets_TrajetId",
                        column: x => x.TrajetId,
                        principalTable: "trajets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commandes_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commandes_utilisateurs_ClientId",
                        column: x => x.ClientId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "avis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<int>(type: "integer", nullable: false),
                    Commentaire = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_avis_commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_avis_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avis_utilisateurs_ClientId",
                        column: x => x.ClientId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "colis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeColis = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QrCodeData = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CodeRetrait = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PoidsReel = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Statut = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_colis_commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paiements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Statut = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DateEncaissement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceExterne = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValidePar = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paiements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paiements_commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evenements_colis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: false),
                    AncienStatut = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    NouveauStatut = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DateHeure = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActeurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Commentaire = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhotoChemin = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evenements_colis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evenements_colis_colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "colis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evenements_colis_utilisateurs_ActeurId",
                        column: x => x.ActeurId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_avis_ClientId",
                table: "avis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_avis_CommandeId",
                table: "avis",
                column: "CommandeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_avis_TransporteurId",
                table: "avis",
                column: "TransporteurId");

            migrationBuilder.CreateIndex(
                name: "IX_colis_CodeColis",
                table: "colis",
                column: "CodeColis",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_colis_CommandeId",
                table: "colis",
                column: "CommandeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_colis_Statut",
                table: "colis",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_ClientId",
                table: "commandes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_DateCreation",
                table: "commandes",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_RelaisArriveeId",
                table: "commandes",
                column: "RelaisArriveeId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_RelaisDepartId",
                table: "commandes",
                column: "RelaisDepartId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_TrajetId",
                table: "commandes",
                column: "TrajetId");

            migrationBuilder.CreateIndex(
                name: "IX_commandes_TransporteurId",
                table: "commandes",
                column: "TransporteurId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_kyc_TransporteurId",
                table: "documents_kyc",
                column: "TransporteurId");

            migrationBuilder.CreateIndex(
                name: "IX_evenements_colis_ActeurId",
                table: "evenements_colis",
                column: "ActeurId");

            migrationBuilder.CreateIndex(
                name: "IX_evenements_colis_ColisId",
                table: "evenements_colis",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_evenements_colis_DateHeure",
                table: "evenements_colis",
                column: "DateHeure");

            migrationBuilder.CreateIndex(
                name: "IX_paiements_CommandeId",
                table: "paiements",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_paiements_Statut",
                table: "paiements",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_points_relais_Pays_Ville",
                table: "points_relais",
                columns: new[] { "Pays", "Ville" });

            migrationBuilder.CreateIndex(
                name: "IX_points_relais_UtilisateurId",
                table: "points_relais",
                column: "UtilisateurId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trajets_Statut",
                table: "trajets",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_trajets_TransporteurId",
                table: "trajets",
                column: "TransporteurId");

            migrationBuilder.CreateIndex(
                name: "IX_trajets_VilleDepart_VilleArrivee_DateDepart",
                table: "trajets",
                columns: new[] { "VilleDepart", "VilleArrivee", "DateDepart" });

            migrationBuilder.CreateIndex(
                name: "IX_transporteurs_StatutKyc",
                table: "transporteurs",
                column: "StatutKyc");

            migrationBuilder.CreateIndex(
                name: "IX_transporteurs_UtilisateurId",
                table: "transporteurs",
                column: "UtilisateurId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilisateurs_Email",
                table: "utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilisateurs_Role",
                table: "utilisateurs",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avis");

            migrationBuilder.DropTable(
                name: "documents_kyc");

            migrationBuilder.DropTable(
                name: "evenements_colis");

            migrationBuilder.DropTable(
                name: "paiements");

            migrationBuilder.DropTable(
                name: "colis");

            migrationBuilder.DropTable(
                name: "commandes");

            migrationBuilder.DropTable(
                name: "points_relais");

            migrationBuilder.DropTable(
                name: "trajets");

            migrationBuilder.DropTable(
                name: "transporteurs");

            migrationBuilder.DropTable(
                name: "utilisateurs");
        }
    }
}
