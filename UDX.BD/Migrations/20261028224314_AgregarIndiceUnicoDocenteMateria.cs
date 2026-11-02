using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnicoDocenteMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocentesMaterias_TeacherId",
                table: "DocentesMaterias");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "DocentesMaterias",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_TeacherId_MateriaId_Level",
                table: "DocentesMaterias",
                columns: new[] { "TeacherId", "MateriaId", "Level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocentesMaterias_TeacherId_MateriaId_Level",
                table: "DocentesMaterias");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "DocentesMaterias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_TeacherId",
                table: "DocentesMaterias",
                column: "TeacherId");
        }
    }
}
