using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTarifEtChampsValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HauteurMaxColisCm",
                table: "trajets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LargeurMaxColisCm",
                table: "trajets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LongueurMaxColisCm",
                table: "trajets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PoidsMaxColisKg",
                table: "trajets",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TarifId",
                table: "trajets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HauteurCm",
                table: "commandes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LargeurCm",
                table: "commandes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LongueurCm",
                table: "commandes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tarifs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrixAuKiloStandard = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SeuilStandardKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ForfaitLourd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PrixAuKiloLourd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ForfaitHorsGabarit = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PrixAuKiloHorsGabarit = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LongueurMaxStandardCm = table.Column<int>(type: "integer", nullable: false),
                    LargeurMaxStandardCm = table.Column<int>(type: "integer", nullable: false),
                    HauteurMaxStandardCm = table.Column<int>(type: "integer", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tarifs_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trajets_TarifId",
                table: "trajets",
                column: "TarifId");

            migrationBuilder.CreateIndex(
                name: "IX_tarifs_TransporteurId",
                table: "tarifs",
                column: "TransporteurId");

            migrationBuilder.AddForeignKey(
                name: "FK_trajets_tarifs_TarifId",
                table: "trajets",
                column: "TarifId",
                principalTable: "tarifs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trajets_tarifs_TarifId",
                table: "trajets");

            migrationBuilder.DropTable(
                name: "tarifs");

            migrationBuilder.DropIndex(
                name: "IX_trajets_TarifId",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "HauteurMaxColisCm",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "LargeurMaxColisCm",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "LongueurMaxColisCm",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "PoidsMaxColisKg",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "TarifId",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "HauteurCm",
                table: "commandes");

            migrationBuilder.DropColumn(
                name: "LargeurCm",
                table: "commandes");

            migrationBuilder.DropColumn(
                name: "LongueurCm",
                table: "commandes");
        }
    }
}
