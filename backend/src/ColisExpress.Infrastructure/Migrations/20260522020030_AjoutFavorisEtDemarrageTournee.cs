using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFavorisEtDemarrageTournee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDemarrageTournee",
                table: "trajets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "favoris",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointRelaisId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransporteurId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favoris", x => x.Id);
                    table.ForeignKey(
                        name: "FK_favoris_points_relais_PointRelaisId",
                        column: x => x.PointRelaisId,
                        principalTable: "points_relais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_favoris_transporteurs_TransporteurId",
                        column: x => x.TransporteurId,
                        principalTable: "transporteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_favoris_utilisateurs_ClientId",
                        column: x => x.ClientId,
                        principalTable: "utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_favoris_ClientId",
                table: "favoris",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_favoris_ClientId_PointRelaisId",
                table: "favoris",
                columns: new[] { "ClientId", "PointRelaisId" });

            migrationBuilder.CreateIndex(
                name: "IX_favoris_ClientId_TransporteurId",
                table: "favoris",
                columns: new[] { "ClientId", "TransporteurId" });

            migrationBuilder.CreateIndex(
                name: "IX_favoris_PointRelaisId",
                table: "favoris",
                column: "PointRelaisId");

            migrationBuilder.CreateIndex(
                name: "IX_favoris_TransporteurId",
                table: "favoris",
                column: "TransporteurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favoris");

            migrationBuilder.DropColumn(
                name: "DateDemarrageTournee",
                table: "trajets");
        }
    }
}
