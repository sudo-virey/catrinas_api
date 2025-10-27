using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTotalesPorCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalAtuendo",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInteraccion",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMaquillaje",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPasarela",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTradiciones",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAtuendo",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "TotalInteraccion",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "TotalMaquillaje",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "TotalPasarela",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "TotalTradiciones",
                table: "Rankings");
        }
    }
}
