using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelaEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Medio = table.Column<string>(type: "text", nullable: true),
                    Genero = table.Column<string>(type: "text", nullable: true),
                    Logline = table.Column<string>(type: "text", nullable: true),
                    Intake = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    PlataformaObjetivo = table.Column<string>(type: "text", nullable: true),
                    UniversoCompartido = table.Column<string>(type: "text", nullable: true),
                    TemaCentral = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Beats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Acto = table.Column<string>(type: "text", nullable: false),
                    Funcion = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beats_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Acto = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    MomentoInWorld = table.Column<string>(type: "text", nullable: true),
                    CambioConsecuencia = table.Column<string>(type: "text", nullable: true),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eventos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personajes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rol = table.Column<string>(type: "text", nullable: true),
                    Arquetipo = table.Column<string>(type: "text", nullable: true),
                    HeridaCentral = table.Column<string>(type: "text", nullable: true),
                    Deseo = table.Column<string>(type: "text", nullable: true),
                    Necesidad = table.Column<string>(type: "text", nullable: true),
                    Restricciones = table.Column<string>(type: "text", nullable: true),
                    EstadoEnTrama = table.Column<string>(type: "text", nullable: true),
                    EstadoVital = table.Column<string>(type: "text", nullable: false),
                    Canon = table.Column<string>(type: "text", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personajes_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosPaso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Agente = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    CanonSnapshot = table.Column<string>(type: "text", nullable: true),
                    Cambios = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPaso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosPaso_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: true),
                    RolEnTrama = table.Column<string>(type: "text", nullable: true),
                    EstadoActual = table.Column<string>(type: "text", nullable: true),
                    Canon = table.Column<string>(type: "text", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ubicaciones_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Capitulos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Plataforma = table.Column<string>(type: "text", nullable: true),
                    BeatObjetivoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capitulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capitulos_Beats_BeatObjetivoId",
                        column: x => x.BeatObjetivoId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Capitulos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventoPersonaje",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonajesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoPersonaje", x => new { x.EventoId, x.PersonajesId });
                    table.ForeignKey(
                        name: "FK_EventoPersonaje_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventoPersonaje_Personajes_PersonajesId",
                        column: x => x.PersonajesId,
                        principalTable: "Personajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventoUbicacion",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UbicacionesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoUbicacion", x => new { x.EventoId, x.UbicacionesId });
                    table.ForeignKey(
                        name: "FK_EventoUbicacion_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventoUbicacion_Ubicaciones_UbicacionesId",
                        column: x => x.UbicacionesId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapituloVersiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapituloId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVersion = table.Column<int>(type: "integer", nullable: false),
                    Modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PromptUsado = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    EsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapituloVersiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapituloVersiones_Capitulos_CapituloId",
                        column: x => x.CapituloId,
                        principalTable: "Capitulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PruebasAcido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapituloVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fisica = table.Column<bool>(type: "boolean", nullable: false),
                    Psicologica = table.Column<bool>(type: "boolean", nullable: false),
                    Ambiental = table.Column<bool>(type: "boolean", nullable: false),
                    Quimica = table.Column<bool>(type: "boolean", nullable: false),
                    Veredicto = table.Column<string>(type: "text", nullable: false),
                    Hallazgos = table.Column<string>(type: "text", nullable: true),
                    Parches = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PruebasAcido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PruebasAcido_CapituloVersiones_CapituloVersionId",
                        column: x => x.CapituloVersionId,
                        principalTable: "CapituloVersiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beats_ObraId_Orden",
                table: "Beats",
                columns: new[] { "ObraId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_Capitulos_BeatObjetivoId",
                table: "Capitulos",
                column: "BeatObjetivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Capitulos_ObraId_Orden",
                table: "Capitulos",
                columns: new[] { "ObraId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_CapituloVersiones_CapituloId_NumeroVersion",
                table: "CapituloVersiones",
                columns: new[] { "CapituloId", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventoPersonaje_PersonajesId",
                table: "EventoPersonaje",
                column: "PersonajesId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_ObraId_Orden",
                table: "Eventos",
                columns: new[] { "ObraId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_EventoUbicacion_UbicacionesId",
                table: "EventoUbicacion",
                column: "UbicacionesId");

            migrationBuilder.CreateIndex(
                name: "IX_Personajes_ObraId_Nombre",
                table: "Personajes",
                columns: new[] { "ObraId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_PruebasAcido_CapituloVersionId",
                table: "PruebasAcido",
                column: "CapituloVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPaso_ObraId_Timestamp",
                table: "RegistrosPaso",
                columns: new[] { "ObraId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_ObraId_Nombre",
                table: "Ubicaciones",
                columns: new[] { "ObraId", "Nombre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventoPersonaje");

            migrationBuilder.DropTable(
                name: "EventoUbicacion");

            migrationBuilder.DropTable(
                name: "PruebasAcido");

            migrationBuilder.DropTable(
                name: "RegistrosPaso");

            migrationBuilder.DropTable(
                name: "Personajes");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropTable(
                name: "CapituloVersiones");

            migrationBuilder.DropTable(
                name: "Capitulos");

            migrationBuilder.DropTable(
                name: "Beats");

            migrationBuilder.DropTable(
                name: "Obras");
        }
    }
}
