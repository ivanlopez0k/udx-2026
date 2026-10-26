using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAcademiaAContenido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademyId",
                table: "Contenidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_AcademyId",
                table: "Contenidos",
                column: "AcademyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contenidos_Academias_AcademyId",
                table: "Contenidos",
                column: "AcademyId",
                principalTable: "Academias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contenidos_Academias_AcademyId",
                table: "Contenidos");

            migrationBuilder.DropIndex(
                name: "IX_Contenidos_AcademyId",
                table: "Contenidos");

            migrationBuilder.DropColumn(
                name: "AcademyId",
                table: "Contenidos");
        }
    }
}
