using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmControlAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMinimoInsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimo",
                table: "Insumos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Insumos");
        }
    }
}
