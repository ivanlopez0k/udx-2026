using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFotoPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "PerfilesDocentes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "PerfilesDocentes");
        }
    }
}
