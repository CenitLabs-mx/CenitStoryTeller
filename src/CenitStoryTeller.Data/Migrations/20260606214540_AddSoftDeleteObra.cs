using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenitStoryTeller.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteObra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EliminadaEn",
                table: "Obras",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EliminadaEn",
                table: "Obras");
        }
    }
}
