using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarParticipantesDeEjemplo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Participantes",
                columns: new[] { "Id_Participante", "Activo", "Id_Estado", "Nombre" },
                values: new object[,]
                {
                    { 1, true, 2, "Ana García Martínez" },
                    { 2, true, 2, "Luis Rodríguez López" },
                    { 3, true, 2, "Carmen Flores Sánchez" },
                    { 4, true, 2, "Jorge Hernández Vega" },
                    { 5, true, 2, "María Isabel Jiménez" },
                    { 6, true, 2, "Carlos Eduardo Morales" },
                    { 7, true, 2, "Sofia Alejandra Ruiz" },
                    { 8, true, 2, "Ricardo Daniel Torres" },
                    { 9, true, 1, "Alejandra Beatriz Luna" },
                    { 10, true, 1, "Fernando Javier Castro" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Participantes",
                keyColumn: "Id_Participante",
                keyValue: 10);
        }
    }
}
