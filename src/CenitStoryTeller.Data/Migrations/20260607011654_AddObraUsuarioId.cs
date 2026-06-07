using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenitStoryTeller.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObraUsuarioId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Obras",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Obras");
        }
    }
}
