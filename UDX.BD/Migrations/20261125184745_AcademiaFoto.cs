using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AcademiaFoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Foto",
                table: "Academias",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Foto",
                table: "Academias");
        }
    }
}
