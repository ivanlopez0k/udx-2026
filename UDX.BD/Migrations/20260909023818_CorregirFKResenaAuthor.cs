using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class CorregirFKResenaAuthor : Migration
    {
        // Se corrige la relación Resena-Usuario (Author): la propiedad authorResena no seguía la
        // convención de nombres de EF Core (se esperaba AuthorId), por lo que EF había creado una
        // columna FK oculta separada que nunca se completaba. Esta migración fuerza la relación a
        // usar la columna real authorResena.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resenas_Usuarios_AuthorId",
                table: "Resenas");

            migrationBuilder.DropIndex(
                name: "IX_Resenas_AuthorId",
                table: "Resenas");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Resenas");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_authorResena",
                table: "Resenas",
                column: "authorResena");

            migrationBuilder.AddForeignKey(
                name: "FK_Resenas_Usuarios_authorResena",
                table: "Resenas",
                column: "authorResena",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resenas_Usuarios_authorResena",
                table: "Resenas");

            migrationBuilder.DropIndex(
                name: "IX_Resenas_authorResena",
                table: "Resenas");

            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "Resenas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_AuthorId",
                table: "Resenas",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resenas_Usuarios_AuthorId",
                table: "Resenas",
                column: "AuthorId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }
    }
}
