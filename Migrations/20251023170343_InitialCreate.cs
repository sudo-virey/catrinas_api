using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accesos",
                columns: table => new
                {
                    Id_Acceso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Acceso = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accesos", x => x.Id_Acceso);
                });

            migrationBuilder.CreateTable(
                name: "Ajustes",
                columns: table => new
                {
                    Id_Ajuste = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tiempo_de_Votacion = table.Column<int>(type: "int", nullable: false),
                    Publicacion_Resultados = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ajustes", x => x.Id_Ajuste);
                });

            migrationBuilder.CreateTable(
                name: "Estados",
                columns: table => new
                {
                    Id_Estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estados", x => x.Id_Estado);
                });

            migrationBuilder.CreateTable(
                name: "HistorialAccesos",
                columns: table => new
                {
                    Id_Historial_Acceso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Acceso = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialAccesos", x => x.Id_Historial_Acceso);
                    table.ForeignKey(
                        name: "FK_HistorialAccesos_Accesos_Id_Acceso",
                        column: x => x.Id_Acceso,
                        principalTable: "Accesos",
                        principalColumn: "Id_Acceso",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Id_Acceso = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Accesos_Id_Acceso",
                        column: x => x.Id_Acceso,
                        principalTable: "Accesos",
                        principalColumn: "Id_Acceso",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Participantes",
                columns: table => new
                {
                    Id_Participante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id_Estado = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participantes", x => x.Id_Participante);
                    table.ForeignKey(
                        name: "FK_Participantes_Estados_Id_Estado",
                        column: x => x.Id_Estado,
                        principalTable: "Estados",
                        principalColumn: "Id_Estado",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evaluaciones",
                columns: table => new
                {
                    Id_Evaluacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Participante = table.Column<int>(type: "int", nullable: false),
                    Id_Acceso = table.Column<int>(type: "int", nullable: false),
                    Atuendo = table.Column<int>(type: "int", nullable: false),
                    Maquillaje = table.Column<int>(type: "int", nullable: false),
                    Tradiciones = table.Column<int>(type: "int", nullable: false),
                    Pasarela = table.Column<int>(type: "int", nullable: false),
                    Interaccion = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluaciones", x => x.Id_Evaluacion);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Accesos_Id_Acceso",
                        column: x => x.Id_Acceso,
                        principalTable: "Accesos",
                        principalColumn: "Id_Acceso",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Participantes_Id_Participante",
                        column: x => x.Id_Participante,
                        principalTable: "Participantes",
                        principalColumn: "Id_Participante",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Accesos",
                columns: new[] { "Id_Acceso", "Acceso" },
                values: new object[,]
                {
                    { 1, "ADM001" },
                    { 2, "JUE001" },
                    { 3, "JUE002" },
                    { 4, "JUE003" }
                });

            migrationBuilder.InsertData(
                table: "Ajustes",
                columns: new[] { "Id_Ajuste", "Activo", "Fecha", "Publicacion_Resultados", "Tiempo_de_Votacion" },
                values: new object[] { 1, true, new DateTime(2025, 10, 23, 11, 3, 43, 511, DateTimeKind.Local).AddTicks(2260), false, 30 });

            migrationBuilder.InsertData(
                table: "Estados",
                columns: new[] { "Id_Estado", "Estado" },
                values: new object[,]
                {
                    { 1, "Aguascalientes" },
                    { 2, "Baja California" },
                    { 3, "Baja California Sur" },
                    { 4, "Campeche" },
                    { 5, "Chiapas" },
                    { 6, "Chihuahua" },
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

            migrationBuilder.CreateIndex(
                name: "IX_Accesos_Acceso",
                table: "Accesos",
                column: "Acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estados_Estado",
                table: "Estados",
                column: "Estado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_Id_Acceso",
                table: "Evaluaciones",
                column: "Id_Acceso");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_Id_Participante_Id_Acceso",
                table: "Evaluaciones",
                columns: new[] { "Id_Participante", "Id_Acceso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAccesos_Id_Acceso",
                table: "HistorialAccesos",
                column: "Id_Acceso");

            migrationBuilder.CreateIndex(
                name: "IX_Participantes_Id_Estado",
                table: "Participantes",
                column: "Id_Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Id_Acceso",
                table: "Usuarios",
                column: "Id_Acceso");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ajustes");

            migrationBuilder.DropTable(
                name: "Evaluaciones");

            migrationBuilder.DropTable(
                name: "HistorialAccesos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Participantes");

            migrationBuilder.DropTable(
                name: "Accesos");

            migrationBuilder.DropTable(
                name: "Estados");
        }
    }
}
