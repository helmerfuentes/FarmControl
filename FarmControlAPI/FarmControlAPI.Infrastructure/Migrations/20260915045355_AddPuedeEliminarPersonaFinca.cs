using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPuedeEliminarPersonaFinca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PuedeEliminar",
                table: "PersonasFincas",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuedeEliminar",
                table: "PersonasFincas");
        }
    }
}
