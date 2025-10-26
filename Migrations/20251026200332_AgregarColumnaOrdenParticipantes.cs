using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarColumnaOrdenParticipantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Participantes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 1,
                column: "Orden",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 2,
                column: "Orden",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 3,
                column: "Orden",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 4,
                column: "Orden",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 5,
                column: "Orden",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 6,
                column: "Orden",
                value: 6);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 7,
                column: "Orden",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 8,
                column: "Orden",
                value: 8);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 9,
                column: "Orden",
                value: 9);

            migrationBuilder.UpdateData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 10,
                column: "Orden",
                value: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Participantes");
        }
    }
}
