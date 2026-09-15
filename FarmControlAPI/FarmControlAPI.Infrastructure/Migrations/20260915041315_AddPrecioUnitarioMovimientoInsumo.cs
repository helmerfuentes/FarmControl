using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecioUnitarioMovimientoInsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioUnitario",
                table: "MovimientosInsumo",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioUnitario",
                table: "MovimientosInsumo");
        }
    }
}
