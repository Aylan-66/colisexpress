using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCoordonneesRelaisEtSoldeEspeces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "points_relais",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "points_relais",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateReversement",
                table: "paiements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstReverseAdmin",
                table: "paiements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelaisEncaisseurId",
                table: "paiements",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "DateReversement",
                table: "paiements");

            migrationBuilder.DropColumn(
                name: "EstReverseAdmin",
                table: "paiements");

            migrationBuilder.DropColumn(
                name: "RelaisEncaisseurId",
                table: "paiements");
        }
    }
}
