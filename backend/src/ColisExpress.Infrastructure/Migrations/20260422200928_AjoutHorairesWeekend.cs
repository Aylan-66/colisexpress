using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutHorairesWeekend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureFermetureWeekend",
                table: "points_relais",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureOuvertureWeekend",
                table: "points_relais",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeureFermetureWeekend",
                table: "points_relais");

            migrationBuilder.DropColumn(
                name: "HeureOuvertureWeekend",
                table: "points_relais");
        }
    }
}
