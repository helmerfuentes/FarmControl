using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add13NuevasFuncionalidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Insumos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Confirmada",
                table: "Actividades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmadaPor",
                table: "Actividades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacion",
                table: "Actividades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PagosVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportesCompartidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    FincaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoPorPersonaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesCompartidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesCompartidos_Fincas_FincaId",
                        column: x => x.FincaId,
                        principalTable: "Fincas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TareasRecurrentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParcelaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    FrecuenciaDias = table.Column<int>(type: "INTEGER", nullable: false),
                    ProximaFecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaEjecucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasRecurrentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareasRecurrentes_Parcelas_ParcelaId",
                        column: x => x.ParcelaId,
                        principalTable: "Parcelas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosVenta_VentaId",
                table: "PagosVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesCompartidos_FincaId",
                table: "ReportesCompartidos",
                column: "FincaId");

            migrationBuilder.CreateIndex(
                name: "IX_TareasRecurrentes_ParcelaId",
                table: "TareasRecurrentes",
                column: "ParcelaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosVenta");

            migrationBuilder.DropTable(
                name: "ReportesCompartidos");

            migrationBuilder.DropTable(
                name: "TareasRecurrentes");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "Confirmada",
                table: "Actividades");

            migrationBuilder.DropColumn(
                name: "ConfirmadaPor",
                table: "Actividades");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacion",
                table: "Actividades");
        }
    }
}
