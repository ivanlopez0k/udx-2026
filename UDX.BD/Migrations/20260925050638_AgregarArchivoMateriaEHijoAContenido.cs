using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarArchivoMateriaEHijoAContenido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Contenidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HijoId",
                table: "Contenidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MateriaId",
                table: "Contenidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_HijoId",
                table: "Contenidos",
                column: "HijoId");

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_MateriaId",
                table: "Contenidos",
                column: "MateriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contenidos_Hijos_HijoId",
                table: "Contenidos",
                column: "HijoId",
                principalTable: "Hijos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contenidos_Materias_MateriaId",
                table: "Contenidos",
                column: "MateriaId",
                principalTable: "Materias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contenidos_Hijos_HijoId",
                table: "Contenidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Contenidos_Materias_MateriaId",
                table: "Contenidos");

            migrationBuilder.DropIndex(
                name: "IX_Contenidos_HijoId",
                table: "Contenidos");

            migrationBuilder.DropIndex(
                name: "IX_Contenidos_MateriaId",
                table: "Contenidos");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Contenidos");

            migrationBuilder.DropColumn(
                name: "HijoId",
                table: "Contenidos");

            migrationBuilder.DropColumn(
                name: "MateriaId",
                table: "Contenidos");
        }
    }
}
