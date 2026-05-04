using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutRelaisDepartTrajet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelaisDepartId",
                table: "trajets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_trajets_RelaisDepartId",
                table: "trajets",
                column: "RelaisDepartId");

            migrationBuilder.AddForeignKey(
                name: "FK_trajets_points_relais_RelaisDepartId",
                table: "trajets",
                column: "RelaisDepartId",
                principalTable: "points_relais",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trajets_points_relais_RelaisDepartId",
                table: "trajets");

            migrationBuilder.DropIndex(
                name: "IX_trajets_RelaisDepartId",
                table: "trajets");

            migrationBuilder.DropColumn(
                name: "RelaisDepartId",
                table: "trajets");
        }
    }
}
