using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class AcademiaCodeAndAcademyIdFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Code column as nullable
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Academias",
                type: "nvarchar(450)",
                nullable: true);

            // Step 2a: Backfill missing codes (8-char uppercase alphanumeric)
            migrationBuilder.Sql(@"
                UPDATE Academias
                SET Code = UPPER(SUBSTRING(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', ''), 1, 8))
                WHERE Code IS NULL OR Code = '';
            ");

            // Step 2b: Link orphan teachers to an academy they already own
            migrationBuilder.Sql(@"
                UPDATE p
                SET AcademyId = (SELECT TOP 1 a.Id FROM Academias a WHERE a.TeacherId = p.UserId ORDER BY a.Id)
                FROM PerfilesDocentes p
                WHERE p.AcademyId IS NULL
                  AND EXISTS (SELECT 1 FROM Academias a WHERE a.TeacherId = p.UserId);
            ");

            // Step 2c: Create a default academy for still-orphan teachers
            migrationBuilder.Sql(@"
                INSERT INTO Academias (Name, Description, Code, CreatedAt, TeacherId)
                SELECT CONCAT(u.Name, ' Academy'), '', UPPER(SUBSTRING(REPLACE(CAST(NEWID() AS nvarchar(36)), '-', ''), 1, 8)), GETUTCDATE(), u.Id
                FROM PerfilesDocentes p
                INNER JOIN Usuarios u ON u.Id = p.UserId
                WHERE p.AcademyId IS NULL;
            ");

            // Step 2d: Link the newly created academies (latest owned academy wins)
            migrationBuilder.Sql(@"
                UPDATE p
                SET AcademyId = (SELECT TOP 1 a.Id FROM Academias a WHERE a.TeacherId = p.UserId ORDER BY a.Id DESC)
                FROM PerfilesDocentes p
                WHERE p.AcademyId IS NULL;
            ");

            // Step 3: Constrain Code and AcademyId to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Academias",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Step 4: Unique index on Code
            migrationBuilder.CreateIndex(
                name: "IX_Academias_Code",
                table: "Academias",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Academias_Code",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Academias");

            migrationBuilder.AlterColumn<int>(
                name: "AcademyId",
                table: "PerfilesDocentes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
