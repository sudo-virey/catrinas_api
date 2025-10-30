using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarColumnaEsJuradoEnAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsJurado",
                table: "Accesos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Accesos",
                keyColumn: "Id_Acceso",
                keyValue: 1,
                column: "EsJurado",
                value: false);

            migrationBuilder.UpdateData(
                table: "Accesos",
                keyColumn: "Id_Acceso",
                keyValue: 2,
                column: "EsJurado",
                value: false);

            migrationBuilder.UpdateData(
                table: "Accesos",
                keyColumn: "Id_Acceso",
                keyValue: 3,
                column: "EsJurado",
                value: false);

            migrationBuilder.UpdateData(
                table: "Accesos",
                keyColumn: "Id_Acceso",
                keyValue: 4,
                column: "EsJurado",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsJurado",
                table: "Accesos");
        }
    }
}
