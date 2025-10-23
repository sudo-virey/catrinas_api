using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatrinasAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixedSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ajustes",
                keyColumn: "Id_Ajuste",
                keyValue: 1,
                column: "Fecha",
                value: new DateTime(2025, 10, 23, 12, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ajustes",
                keyColumn: "Id_Ajuste",
                keyValue: 1,
                column: "Fecha",
                value: new DateTime(2025, 10, 23, 11, 3, 43, 511, DateTimeKind.Local).AddTicks(2260));
        }
    }
}
