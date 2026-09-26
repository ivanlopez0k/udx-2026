using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class FixSolicitudClaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesClases_PerfilesPadres_parentId",
                table: "SolicitudesClases");

            migrationBuilder.RenameColumn(
                name: "parentId",
                table: "SolicitudesClases",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_SolicitudesClases_parentId",
                table: "SolicitudesClases",
                newName: "IX_SolicitudesClases_ParentId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "SolicitudesClases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Duration: datetime2 → int via drop/add/rename (no implicit conversion)
            migrationBuilder.AddColumn<int>(
                name: "DurationInt",
                table: "SolicitudesClases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "SolicitudesClases");

            migrationBuilder.RenameColumn(
                name: "DurationInt",
                table: "SolicitudesClases",
                newName: "Duration");

            migrationBuilder.AddColumn<int>(
                name: "Modalidad",
                table: "SolicitudesClases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesClases_PerfilesPadres_ParentId",
                table: "SolicitudesClases",
                column: "ParentId",
                principalTable: "PerfilesPadres",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesClases_PerfilesPadres_ParentId",
                table: "SolicitudesClases");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "SolicitudesClases");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "SolicitudesClases",
                newName: "parentId");

            migrationBuilder.RenameIndex(
                name: "IX_SolicitudesClases_ParentId",
                table: "SolicitudesClases",
                newName: "IX_SolicitudesClases_parentId");

            migrationBuilder.AlterColumn<int>(
                name: "Notes",
                table: "SolicitudesClases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Duration",
                table: "SolicitudesClases",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesClases_PerfilesPadres_parentId",
                table: "SolicitudesClases",
                column: "parentId",
                principalTable: "PerfilesPadres",
                principalColumn: "Id");
        }
    }
}
