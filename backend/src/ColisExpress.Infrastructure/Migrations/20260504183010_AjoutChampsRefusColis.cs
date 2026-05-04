using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutChampsRefusColis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateRefus",
                table: "colis",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifRefus",
                table: "colis",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RefusInspecteAdmin",
                table: "colis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RefusInspectePar",
                table: "colis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefusInspectionDate",
                table: "colis",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefusParRole",
                table: "colis",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefusParUtilisateurId",
                table: "colis",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateRefus",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "MotifRefus",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "RefusInspecteAdmin",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "RefusInspectePar",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "RefusInspectionDate",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "RefusParRole",
                table: "colis");

            migrationBuilder.DropColumn(
                name: "RefusParUtilisateurId",
                table: "colis");
        }
    }
}
