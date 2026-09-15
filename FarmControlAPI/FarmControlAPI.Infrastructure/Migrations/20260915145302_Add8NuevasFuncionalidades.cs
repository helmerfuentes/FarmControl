using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add8NuevasFuncionalidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferenciasDashboardJson",
                table: "Personas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "Clientes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalisisSuelo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParcelaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ph = table.Column<decimal>(type: "TEXT", nullable: true),
                    MateriaOrganica = table.Column<decimal>(type: "TEXT", nullable: true),
                    Nitrogeno = table.Column<decimal>(type: "TEXT", nullable: true),
                    Fosforo = table.Column<decimal>(type: "TEXT", nullable: true),
                    Potasio = table.Column<decimal>(type: "TEXT", nullable: true),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisisSuelo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalisisSuelo_Parcelas_ParcelaId",
                        column: x => x.ParcelaId,
                        principalTable: "Parcelas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TipoEntidad = table.Column<int>(type: "INTEGER", nullable: false),
                    EntidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorPersonaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comentarios_Personas_AutorPersonaId",
                        column: x => x.AutorPersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    MaxFincas = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUsuarios = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_PlanId",
                table: "Clientes",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisSuelo_ParcelaId",
                table: "AnalisisSuelo",
                column: "ParcelaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_AutorPersonaId",
                table: "Comentarios",
                column: "AutorPersonaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Planes_PlanId",
                table: "Clientes",
                column: "PlanId",
                principalTable: "Planes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Planes_PlanId",
                table: "Clientes");

            migrationBuilder.DropTable(
                name: "AnalisisSuelo");

            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "Planes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_PlanId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "PreferenciasDashboardJson",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Clientes");
        }
    }
}
