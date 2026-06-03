using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelaEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChecksModernizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Anacronismo",
                table: "PruebasAcido",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FidelidadFuncional",
                table: "PruebasAcido",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anacronismo",
                table: "PruebasAcido");

            migrationBuilder.DropColumn(
                name: "FidelidadFuncional",
                table: "PruebasAcido");
        }
    }
}
