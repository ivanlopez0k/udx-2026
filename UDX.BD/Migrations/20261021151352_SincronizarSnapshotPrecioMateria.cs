using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarSnapshotPrecioMateria : Migration
    {
        // Vacía a propósito: la columna DocentesMaterias.HourlyRate ya la agrega
        // AgregarPrecioPorMateria. Esta migración solo corrige el snapshot,
        // que la había perdido al resolver un conflicto de merge.
        protected override void Up(MigrationBuilder migrationBuilder) { }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}