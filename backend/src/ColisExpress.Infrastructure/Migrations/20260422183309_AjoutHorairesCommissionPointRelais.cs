using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutHorairesCommissionPointRelais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureFermeture",
                table: "points_relais",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureOuverture",
                table: "points_relais",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JoursOuverture",
                table: "points_relais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantCommission",
                table: "points_relais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TypeCommission",
                table: "points_relais",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeureFermeture",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "HeureOuverture",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "JoursOuverture",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "MontantCommission",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "TypeCommission",
                table: "points_relais");
        }
    }
}
