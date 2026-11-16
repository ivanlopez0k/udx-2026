using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNivelSolicitudClase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "SolicitudesClases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "SolicitudesClases");
        }
    }
}
