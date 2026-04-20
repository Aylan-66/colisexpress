using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColisExpress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutContenuDocumentKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "documents_kyc",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "ContenuFichier",
                table: "documents_kyc",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "documents_kyc");

            migrationBuilder.DropColumn(
                name: "ContenuFichier",
                table: "documents_kyc");
        }
    }
}
