using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CenitStoryTeller.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionLlm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesLlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModelDraft = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ModelReview = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ActualizadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesLlm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesLlm_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesLlm_UsuarioId",
                table: "ConfiguracionesLlm",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesLlm");
        }
    }
}
