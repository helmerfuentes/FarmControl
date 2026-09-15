using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add6NuevasFuncionalidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LiquidacionNominaId",
                table: "RegistrosManoObra",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                table: "Compras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FincaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HoraEntrada = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    HoraSalida = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asistencias_Fincas_FincaId",
                        column: x => x.FincaId,
                        principalTable: "Fincas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiquidacionesNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JornaleroId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalHoras = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalPagar = table.Column<decimal>(type: "TEXT", nullable: false),
                    FechaLiquidacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionesNomina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionesNomina_Personas_JornaleroId",
                        column: x => x.JornaleroId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagosCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosManoObra_LiquidacionNominaId",
                table: "RegistrosManoObra",
                column: "LiquidacionNominaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_FincaId",
                table: "Asistencias",
                column: "FincaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_PersonaId",
                table: "Asistencias",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesNomina_JornaleroId",
                table: "LiquidacionesNomina",
                column: "JornaleroId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosCompra_CompraId",
                table: "PagosCompra",
                column: "CompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosManoObra_LiquidacionesNomina_LiquidacionNominaId",
                table: "RegistrosManoObra",
                column: "LiquidacionNominaId",
                principalTable: "LiquidacionesNomina",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosManoObra_LiquidacionesNomina_LiquidacionNominaId",
                table: "RegistrosManoObra");

            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "LiquidacionesNomina");

            migrationBuilder.DropTable(
                name: "PagosCompra");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosManoObra_LiquidacionNominaId",
                table: "RegistrosManoObra");

            migrationBuilder.DropColumn(
                name: "LiquidacionNominaId",
                table: "RegistrosManoObra");

            migrationBuilder.DropColumn(
                name: "Proveedor",
                table: "Compras");
        }
    }
}
