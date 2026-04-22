using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtapeTrajet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etapes_trajets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrajetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointRelaisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    HeureEstimeeArrivee = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HeureReelleArrivee = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelaisOuvertALArrivee = table.Column<bool>(type: "boolean", nullable: false),
                    Statut = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapes_trajets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_etapes_trajets_points_relais_PointRelaisId",
                        column: x => x.PointRelaisId,
                        principalTable: "points_relais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_etapes_trajets_trajets_TrajetId",
                        column: x => x.TrajetId,
                        principalTable: "trajets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapes_trajets_PointRelaisId",
                table: "etapes_trajets",
                column: "PointRelaisId");

            migrationBuilder.CreateIndex(
                name: "IX_etapes_trajets_TrajetId_Ordre",
                table: "etapes_trajets",
                columns: new[] { "TrajetId", "Ordre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etapes_trajets");
        }
    }
}
