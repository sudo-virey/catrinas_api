using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPuntosDesempateRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PuntosDesempate",
                table: "Rankings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuntosDesempate",
                table: "Rankings");
        }
    }
}
