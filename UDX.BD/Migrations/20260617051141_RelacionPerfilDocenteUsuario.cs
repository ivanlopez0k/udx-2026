using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class RelacionPerfilDocenteUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesDocentes_AcademyId",
                table: "PerfilesDocentes",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesDocentes_UserId",
                table: "PerfilesDocentes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilesDocentes_Academias_AcademyId",
                table: "PerfilesDocentes",
                column: "AcademyId",
                principalTable: "Academias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilesDocentes_Usuarios_UserId",
                table: "PerfilesDocentes",
                column: "UserId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerfilesDocentes_Academias_AcademyId",
                table: "PerfilesDocentes");

            migrationBuilder.DropForeignKey(
                name: "FK_PerfilesDocentes_Usuarios_UserId",
                table: "PerfilesDocentes");

            migrationBuilder.DropIndex(
                name: "IX_PerfilesDocentes_AcademyId",
                table: "PerfilesDocentes");

            migrationBuilder.DropIndex(
                name: "IX_PerfilesDocentes_UserId",
                table: "PerfilesDocentes");

            migrationBuilder.DropColumn(
                name: "AcademyId",
                table: "PerfilesDocentes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PerfilesDocentes");
        }
    }
}
