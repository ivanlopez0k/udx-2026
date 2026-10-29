using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogoMaterias : Migration
    {
        private static readonly string[] SubjectNames = { "Matemática", "Lengua", "Ciencias Naturales", "Ciencias Sociales", "Inglés" };

        private static string CategoryFor(string name) => name switch
        {
            "Matemática" => "Ciencias Exactas",
            "Lengua" => "Lenguas",
            "Ciencias Naturales" => "Ciencias Naturales",
            "Ciencias Sociales" => "Ciencias Sociales",
            _ => "Idiomas",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var materias = new object[35, 3];
            int i = 0;
            foreach (var name in SubjectNames)
            {
                for (int grade = 1; grade <= 7; grade++)
                {
                    materias[i, 0] = name;
                    materias[i, 1] = grade.ToString();
                    materias[i, 2] = CategoryFor(name);
                    i++;
                }
            }

            migrationBuilder.InsertData(
                table: "Materias",
                columns: new[] { "Name", "Grade", "Category" },
                values: materias);

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Email", "Password", "Role", "Name", "Phone", "CreatedAt" },
                values: new object[,]
                {
                    { "admin@udx.com", "Admin123*", 2, "Admin", "", new DateTime(2026, 8, 24) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Email",
                keyValue: "admin@udx.com");

            foreach (var name in SubjectNames)
            {
                for (int grade = 1; grade <= 7; grade++)
                {
                    migrationBuilder.DeleteData(
                        table: "Materias",
                        keyColumns: new[] { "Name", "Grade" },
                        keyValues: new object[] { name, grade.ToString() });
                }
            }
        }
    }
}
