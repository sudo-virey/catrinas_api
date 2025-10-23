using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuariosAdminYRankingTablas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Estados",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Estados",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rankings",
                columns: table => new
                {
                    Id_Ranking = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Participante = table.Column<int>(type: "int", nullable: false),
                    Puntos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rankings", x => x.Id_Ranking);
                    table.ForeignKey(
                        name: "FK_Rankings_Participantes_Id_Participante",
                        column: x => x.Id_Participante,
                        principalTable: "Participantes",
                        principalColumn: "Id_Participante",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosAdmin",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAdmin", x => x.Id_Usuario);
                });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 1,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante registrado en el concurso", "Registrado" });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 2,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante en espera de evaluación", "En Espera" });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 3,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante siendo evaluado por jueces", "En Votación" });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 4,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante ya calificado por todos los jueces", "Calificado" });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 5,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante descalificado del concurso", "Descalificado" });

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 6,
                columns: new[] { "Descripcion", "Estado" },
                values: new object[] { "Participante clasificado como finalista", "Finalista" });

            migrationBuilder.InsertData(
                table: "UsuariosAdmin",
                columns: new[] { "Id_Usuario", "Activo", "Email", "FechaCreacion", "NombreCompleto", "Password", "Rol", "Usuario" },
                values: new object[] { 1, true, "admin@catrinas.com", new DateTime(2025, 10, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Administrador del Sistema", "admin123", "Administrador", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_Id_Participante",
                table: "Rankings",
                column: "Id_Participante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAdmin_Email",
                table: "UsuariosAdmin",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAdmin_Usuario",
                table: "UsuariosAdmin",
                column: "Usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rankings");

            migrationBuilder.DropTable(
                name: "UsuariosAdmin");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Estados");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Estados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 1,
                column: "Estado",
                value: "Aguascalientes");

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 2,
                column: "Estado",
                value: "Baja California");

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 3,
                column: "Estado",
                value: "Baja California Sur");

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 4,
                column: "Estado",
                value: "Campeche");

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 5,
                column: "Estado",
                value: "Chiapas");

            migrationBuilder.UpdateData(
                table: "Estados",
                keyColumn: "Id_Estado",
                keyValue: 6,
                column: "Estado",
                value: "Chihuahua");

            migrationBuilder.InsertData(
                table: "Estados",
                columns: new[] { "Id_Estado", "Estado" },
                values: new object[,]
                {
                    { 7, "Ciudad de México" },
                    { 8, "Coahuila" },
                    { 9, "Colima" },
                    { 10, "Durango" },
                    { 11, "Guanajuato" },
                    { 12, "Guerrero" },
                    { 13, "Hidalgo" },
                    { 14, "Jalisco" },
                    { 15, "México" },
                    { 16, "Michoacán" },
                    { 17, "Morelos" },
                    { 18, "Nayarit" },
                    { 19, "Nuevo León" },
                    { 20, "Oaxaca" },
                    { 21, "Puebla" },
                    { 22, "Querétaro" },
                    { 23, "Quintana Roo" },
                    { 24, "San Luis Potosí" },
                    { 25, "Sinaloa" },
                    { 26, "Sonora" },
                    { 27, "Tabasco" },
                    { 28, "Tamaulipas" },
                    { 29, "Tlaxcala" },
                    { 30, "Veracruz" },
                    { 31, "Yucatán" },
                    { 32, "Zacatecas" }
                });
        }
    }
}
