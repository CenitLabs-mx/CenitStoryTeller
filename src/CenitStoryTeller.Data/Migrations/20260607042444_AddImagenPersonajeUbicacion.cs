using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenitStoryTeller.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImagenPersonajeUbicacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ImagenBytes",
                table: "Ubicaciones",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenContentType",
                table: "Ubicaciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImagenBytes",
                table: "Personajes",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenContentType",
                table: "Personajes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenBytes",
                table: "Ubicaciones");

            migrationBuilder.DropColumn(
                name: "ImagenContentType",
                table: "Ubicaciones");

            migrationBuilder.DropColumn(
                name: "ImagenBytes",
                table: "Personajes");

            migrationBuilder.DropColumn(
                name: "ImagenContentType",
                table: "Personajes");
        }
    }
}
