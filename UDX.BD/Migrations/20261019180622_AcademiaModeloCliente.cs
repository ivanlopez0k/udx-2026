using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AcademiaModeloCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Paso 1: Academias — columnas nuevas y TeacherId -> ResponsableId (sin perder datos)
            migrationBuilder.AddColumn<bool>(
                name: "IsPersonal",
                table: "Academias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Academias",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Academias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Academias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Academias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Academias",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerCourse",
                table: "Academias",
                type: "decimal(18,2)",
                nullable: true);

            // La FK y el índice se sacan para poder renombrar la columna y hacerla nullable;
            // se recrean abajo con los nombres nuevos.
            migrationBuilder.DropForeignKey(
                name: "FK_Academias_Usuarios_TeacherId",
                table: "Academias");

            migrationBuilder.DropIndex(
                name: "IX_Academias_TeacherId",
                table: "Academias");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "Academias",
                newName: "ResponsableId");

            migrationBuilder.AlterColumn<int>(
                name: "ResponsableId",
                table: "Academias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Academias_ResponsableId",
                table: "Academias",
                column: "ResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Academias_Usuarios_ResponsableId",
                table: "Academias",
                column: "ResponsableId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            // Paso 2: tabla de membresías
            migrationBuilder.CreateTable(
                name: "DocentesAcademias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocentesAcademias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocentesAcademias_Academias_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocentesAcademias_PerfilesDocentes_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocentesAcademias_AcademyId",
                table: "DocentesAcademias",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocentesAcademias_TeacherId_AcademyId",
                table: "DocentesAcademias",
                columns: new[] { "TeacherId", "AcademyId" },
                unique: true);

            // Paso 3: cada docente queda como miembro activo de la academia de su AcademyId actual
            migrationBuilder.Sql(@"
                INSERT INTO DocentesAcademias (TeacherId, AcademyId, JoinedAt, IsActive)
                SELECT p.Id, p.AcademyId, GETUTCDATE(), 1
                FROM PerfilesDocentes p
                WHERE p.AcademyId IS NOT NULL;
            ");

            // Paso 4: detectar academias personales ya existentes (las creaba Register como
            // '{Name} Academy', sin descripción y sin miembros que no sean su responsable)
            migrationBuilder.Sql(@"
                UPDATE a
                SET IsPersonal = 1
                FROM Academias a
                INNER JOIN Usuarios u ON u.Id = a.ResponsableId
                WHERE a.Name = CONCAT(u.Name, ' Academy')
                  AND (a.Description IS NULL OR a.Description = '')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM DocentesAcademias da
                      INNER JOIN PerfilesDocentes p ON p.Id = da.TeacherId
                      WHERE da.AcademyId = a.Id
                        AND p.UserId <> a.ResponsableId);
            ");

            // Paso 5a: crear academia personal a los docentes que no son miembros de ninguna
            // personal, salvo que ya exista una personal suya sin miembros
            migrationBuilder.Sql(@"
                INSERT INTO Academias (Name, Description, Code, CreatedAt, ResponsableId, IsPersonal, IsActive)
                SELECT CONCAT(u.Name, ' Academy'), '', UPPER(SUBSTRING(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', ''), 1, 8)), GETUTCDATE(), u.Id, 1, 1
                FROM PerfilesDocentes p
                INNER JOIN Usuarios u ON u.Id = p.UserId
                WHERE NOT EXISTS (
                      SELECT 1
                      FROM DocentesAcademias da
                      INNER JOIN Academias a ON a.Id = da.AcademyId
                      WHERE da.TeacherId = p.Id AND a.IsPersonal = 1)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Academias a
                      WHERE a.ResponsableId = u.Id
                        AND a.IsPersonal = 1
                        AND NOT EXISTS (SELECT 1 FROM DocentesAcademias da WHERE da.AcademyId = a.Id));
            ");

            // Paso 5b: vincular a cada docente que sigue sin personal con su personal sin miembros
            migrationBuilder.Sql(@"
                INSERT INTO DocentesAcademias (TeacherId, AcademyId, JoinedAt, IsActive)
                SELECT p.Id, x.Id, GETUTCDATE(), 1
                FROM PerfilesDocentes p
                CROSS APPLY (
                    SELECT TOP 1 a.Id
                    FROM Academias a
                    WHERE a.ResponsableId = p.UserId
                      AND a.IsPersonal = 1
                      AND NOT EXISTS (SELECT 1 FROM DocentesAcademias da WHERE da.AcademyId = a.Id)
                    ORDER BY a.Id) x
                WHERE NOT EXISTS (
                      SELECT 1
                      FROM DocentesAcademias da
                      INNER JOIN Academias a ON a.Id = da.AcademyId
                      WHERE da.TeacherId = p.Id AND a.IsPersonal = 1);
            ");

            // Paso 6: recién ahora se elimina PerfilesDocentes.AcademyId
            migrationBuilder.DropForeignKey(
                name: "FK_PerfilesDocentes_Academias_AcademyId",
                table: "PerfilesDocentes");

            migrationBuilder.DropIndex(
                name: "IX_PerfilesDocentes_AcademyId",
                table: "PerfilesDocentes");

            migrationBuilder.DropColumn(
                name: "AcademyId",
                table: "PerfilesDocentes");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Reconstrucción aproximada del esquema anterior. Limitaciones:
        /// - Cada docente vuelve a tener UNA sola academia (AcademyId): la membresía activa más
        ///   antigua en una oficial; si no tiene, su personal; si no, cualquier membresía.
        ///   El resto de las membresías, IsActive/LeftAt, contacto y precios se pierden.
        /// - Las academias personales creadas por Up() quedan como academias comunes.
        /// - TeacherId vuelve a ser NOT NULL: falla si hay academias sin responsable
        ///   (p. ej. tras EliminarCuenta). Hay que asignarles uno antes de revertir.
        /// - Falla si queda algún docente sin ninguna membresía (AcademyId NOT NULL).
        /// - Los usuarios con Rol ACADEMY (3) siguen en la base; el código anterior no conoce ese valor.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE p
                SET AcademyId = (
                    SELECT TOP 1 da.AcademyId
                    FROM DocentesAcademias da
                    INNER JOIN Academias a ON a.Id = da.AcademyId
                    WHERE da.TeacherId = p.Id
                    ORDER BY
                        CASE WHEN da.IsActive = 1 AND a.IsPersonal = 0 THEN 0
                             WHEN a.IsPersonal = 1 THEN 1
                             ELSE 2 END,
                        da.JoinedAt, da.Id)
                FROM PerfilesDocentes p;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesDocentes_AcademyId",
                table: "PerfilesDocentes",
                column: "AcademyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfilesDocentes_Academias_AcademyId",
                table: "PerfilesDocentes",
                column: "AcademyId",
                principalTable: "Academias",
                principalColumn: "Id");

            migrationBuilder.DropTable(
                name: "DocentesAcademias");

            migrationBuilder.DropForeignKey(
                name: "FK_Academias_Usuarios_ResponsableId",
                table: "Academias");

            migrationBuilder.DropIndex(
                name: "IX_Academias_ResponsableId",
                table: "Academias");

            migrationBuilder.RenameColumn(
                name: "ResponsableId",
                table: "Academias",
                newName: "TeacherId");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "Academias",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Academias_TeacherId",
                table: "Academias",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Academias_Usuarios_TeacherId",
                table: "Academias",
                column: "TeacherId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            migrationBuilder.DropColumn(name: "IsPersonal", table: "Academias");
            migrationBuilder.DropColumn(name: "IsActive", table: "Academias");
            migrationBuilder.DropColumn(name: "Location", table: "Academias");
            migrationBuilder.DropColumn(name: "Phone", table: "Academias");
            migrationBuilder.DropColumn(name: "Email", table: "Academias");
            migrationBuilder.DropColumn(name: "PricePerHour", table: "Academias");
            migrationBuilder.DropColumn(name: "PricePerCourse", table: "Academias");
        }
    }
}
