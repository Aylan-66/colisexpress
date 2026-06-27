using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrichirPointsTransporteurEtEtape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodePostal",
                table: "PointsTransporteur",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Horaires",
                table: "PointsTransporteur",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "PointsTransporteur",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PointRelaisId",
                table: "etapes_trajets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PointTransporteurId",
                table: "etapes_trajets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_etapes_trajets_PointTransporteurId",
                table: "etapes_trajets",
                column: "PointTransporteurId");

            migrationBuilder.AddForeignKey(
                name: "FK_etapes_trajets_PointsTransporteur_PointTransporteurId",
                table: "etapes_trajets",
                column: "PointTransporteurId",
                principalTable: "PointsTransporteur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_etapes_trajets_PointsTransporteur_PointTransporteurId",
                table: "etapes_trajets");

            migrationBuilder.DropIndex(
                name: "IX_etapes_trajets_PointTransporteurId",
                table: "etapes_trajets");

            migrationBuilder.DropColumn(
                name: "CodePostal",
                table: "PointsTransporteur");

            migrationBuilder.DropColumn(
                name: "Horaires",
                table: "PointsTransporteur");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "PointsTransporteur");

            migrationBuilder.DropColumn(
                name: "PointTransporteurId",
                table: "etapes_trajets");

            migrationBuilder.AlterColumn<Guid>(
                name: "PointRelaisId",
                table: "etapes_trajets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
