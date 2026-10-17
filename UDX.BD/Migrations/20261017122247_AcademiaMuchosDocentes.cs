using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AcademiaMuchosDocentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Paso 1: nuevas columnas en Academias y TeacherId nullable (creador, no da permisos)
            migrationBuilder.AddColumn<bool>(
                name: "IsPersonal",
                table: "Academias",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "Academias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Paso 2: tabla intermedia DocentesAcademias con FKs e índice único (TeacherId, AcademyId)
            migrationBuilder.CreateTable(
                name: "DocentesAcademias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            // Paso 3: copiar PerfilesDocentes.AcademyId a membresías.
            // DIRECTOR (2) si el docente es el creador de la academia, si no DOCENTE (0).
            migrationBuilder.Sql(@"
                INSERT INTO DocentesAcademias (Role, JoinedAt, TeacherId, AcademyId)
                SELECT CASE WHEN a.TeacherId = p.UserId THEN 2 ELSE 0 END, GETUTCDATE(), p.Id, p.AcademyId
                FROM PerfilesDocentes p
                INNER JOIN Academias a ON a.Id = p.AcademyId;
            ");

            // Paso 4: detectar las academias personales (las que creaba automáticamente el Register):
            // nombre "{usuario creador} Academy", sin descripción y sin miembros distintos de su creador.
            migrationBuilder.Sql(@"
                UPDATE a
                SET IsPersonal = 1
                FROM Academias a
                INNER JOIN Usuarios u ON u.Id = a.TeacherId
                WHERE a.Name = CONCAT(u.Name, ' Academy')
                  AND (a.Description = '' OR a.Description IS NULL)
                  AND NOT EXISTS (
                      SELECT 1 FROM DocentesAcademias da
                      INNER JOIN PerfilesDocentes p ON p.Id = da.TeacherId
                      WHERE da.AcademyId = a.Id AND p.UserId <> a.TeacherId);
            ");

            // Paso 5: en la academia personal nadie es director
            migrationBuilder.Sql(@"
                UPDATE da
                SET Role = 0
                FROM DocentesAcademias da
                INNER JOIN Academias a ON a.Id = da.AcademyId
                WHERE a.IsPersonal = 1;
            ");

            // Paso 6a: crear academia personal a cada docente que todavía no tenga membresía en una
            migrationBuilder.Sql(@"
                INSERT INTO Academias (Name, Description, Code, CreatedAt, TeacherId, IsPersonal)
                SELECT CONCAT(u.Name, ' Academy'), '', UPPER(SUBSTRING(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', ''), 1, 8)), GETUTCDATE(), u.Id, 1
                FROM PerfilesDocentes p
                INNER JOIN Usuarios u ON u.Id = p.UserId
                WHERE NOT EXISTS (
                    SELECT 1 FROM DocentesAcademias da
                    INNER JOIN Academias a ON a.Id = da.AcademyId
                    WHERE da.TeacherId = p.Id AND a.IsPersonal = 1)
                  AND NOT EXISTS (
                    SELECT 1 FROM Academias a2
                    WHERE a2.TeacherId = u.Id AND a2.IsPersonal = 1
                      AND NOT EXISTS (SELECT 1 FROM DocentesAcademias da3 WHERE da3.AcademyId = a2.Id));
            ");

            // Paso 6b: membresía DOCENTE en la academia personal recién creada (la más nueva sin miembros)
            migrationBuilder.Sql(@"
                INSERT INTO DocentesAcademias (Role, JoinedAt, TeacherId, AcademyId)
                SELECT 0, GETUTCDATE(), p.Id, nueva.Id
                FROM PerfilesDocentes p
                CROSS APPLY (
                    SELECT TOP 1 a.Id
                    FROM Academias a
                    WHERE a.TeacherId = p.UserId
                      AND a.IsPersonal = 1
                      AND NOT EXISTS (SELECT 1 FROM DocentesAcademias da2 WHERE da2.AcademyId = a.Id)
                    ORDER BY a.Id DESC) nueva
                WHERE NOT EXISTS (
                    SELECT 1 FROM DocentesAcademias da
                    INNER JOIN Academias a ON a.Id = da.AcademyId
                    WHERE da.TeacherId = p.Id AND a.IsPersonal = 1);
            ");

            // Paso 7: recién ahora se elimina PerfilesDocentes.AcademyId
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
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Paso 1: volver a crear PerfilesDocentes.AcademyId (nullable mientras se puebla)
            migrationBuilder.AddColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: true);

            // Paso 2: academia oficial de mayor rango (y más antigua); si no tiene, la personal
            migrationBuilder.Sql(@"
                UPDATE p
                SET AcademyId = (
                    SELECT TOP 1 da.AcademyId
                    FROM DocentesAcademias da
                    INNER JOIN Academias a ON a.Id = da.AcademyId
                    WHERE da.TeacherId = p.Id
                    ORDER BY a.IsPersonal ASC, da.Role DESC, da.JoinedAt ASC, a.CreatedAt ASC, a.Id ASC)
                FROM PerfilesDocentes p;
            ");

            // Paso 3: las academias personales que no quedaron asignadas a ningún perfil ni las usa
            // ninguna solicitud/clase (p. ej. las que creó el Up() para docentes que ya estaban en una
            // oficial) se eliminan para volver al estado anterior.
            migrationBuilder.Sql(@"
                DELETE da
                FROM DocentesAcademias da
                INNER JOIN Academias a ON a.Id = da.AcademyId
                WHERE a.IsPersonal = 1
                  AND NOT EXISTS (SELECT 1 FROM PerfilesDocentes p WHERE p.AcademyId = a.Id)
                  AND NOT EXISTS (SELECT 1 FROM SolicitudesClases s WHERE s.AcademyId = a.Id)
                  AND NOT EXISTS (SELECT 1 FROM ClasesAgendadas c WHERE c.AcademyId = a.Id);

                DELETE a
                FROM Academias a
                WHERE a.IsPersonal = 1
                  AND NOT EXISTS (SELECT 1 FROM DocentesAcademias da WHERE da.AcademyId = a.Id)
                  AND NOT EXISTS (SELECT 1 FROM PerfilesDocentes p WHERE p.AcademyId = a.Id)
                  AND NOT EXISTS (SELECT 1 FROM SolicitudesClases s WHERE s.AcademyId = a.Id)
                  AND NOT EXISTS (SELECT 1 FROM ClasesAgendadas c WHERE c.AcademyId = a.Id);
            ");

            // Paso 4: TeacherId vuelve a ser obligatorio.
            // LIMITACIÓN: si el creador de una academia se eliminó (TeacherId = null), el Down() no
            // puede recuperar al creador original. Se le asigna el usuario del miembro de mayor rango
            // y más antiguo. Si la academia además quedó sin miembros, no hay a quién asignarla y el
            // Down() se aborta con un error explícito: hay que asignarle un TeacherId o borrarla a
            // mano antes de revertir.
            migrationBuilder.Sql(@"
                UPDATE a
                SET TeacherId = (
                    SELECT TOP 1 p.UserId
                    FROM DocentesAcademias da
                    INNER JOIN PerfilesDocentes p ON p.Id = da.TeacherId
                    WHERE da.AcademyId = a.Id
                    ORDER BY da.Role DESC, da.JoinedAt ASC, da.Id ASC)
                FROM Academias a
                WHERE a.TeacherId IS NULL;

                IF EXISTS (SELECT 1 FROM Academias WHERE TeacherId IS NULL)
                    THROW 50000, 'Down AcademiaMuchosDocentes: hay academias con TeacherId NULL y sin miembros. Asignarles un TeacherId o eliminarlas manualmente antes de revertir.', 1;

                IF EXISTS (SELECT 1 FROM PerfilesDocentes WHERE AcademyId IS NULL)
                    THROW 50001, 'Down AcademiaMuchosDocentes: hay PerfilesDocentes sin ninguna membresia para reconstruir AcademyId.', 1;
            ");

            migrationBuilder.DropTable(
                name: "DocentesAcademias");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "IsPersonal",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Academias");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "Academias",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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
        }
    }
}
