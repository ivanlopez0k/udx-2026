using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class Inicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesDocentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Credentials = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RatingAvg = table.Column<float>(type: "real", nullable: false),
                    RatinCount = table.Column<int>(type: "int", nullable: false),
                    Availability = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesDocentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocentesMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    MateriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocentesMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_PerfilesDocentes_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Academias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Academias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Academias_Usuarios_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Contenidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contenidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contenidos_Usuarios_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Contenidos_Usuarios_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hijos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hijos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hijos_Usuarios_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isRead = table.Column<bool>(type: "bit", nullable: false),
                    senderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensajes_Usuarios_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mensajes_Usuarios_senderId",
                        column: x => x.senderId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Resenas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    authorResena = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resenas_PerfilesDocentes_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resenas_Usuarios_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PerfilesPadres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HijoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesPadres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilesPadres_Hijos_HijoId",
                        column: x => x.HijoId,
                        principalTable: "Hijos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PerfilesPadres_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesClases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Schedule = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    parentId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    DocenteId = table.Column<int>(type: "int", nullable: false),
                    MateriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesClases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesClases_Academias_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitudesClases_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitudesClases_PerfilesDocentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitudesClases_PerfilesPadres_parentId",
                        column: x => x.parentId,
                        principalTable: "PerfilesPadres",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClasesAgendadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<float>(type: "real", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    ContentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasesAgendadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_Academias_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_Contenidos_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contenidos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_Materias_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Materias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_PerfilesDocentes_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_PerfilesPadres_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PerfilesPadres",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClasesAgendadas_SolicitudesClases_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SolicitudesClases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PagosDocentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProcessorPayout = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ClaseAgendadaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosDocentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosDocentes_ClasesAgendadas_ClaseAgendadaId",
                        column: x => x.ClaseAgendadaId,
                        principalTable: "ClasesAgendadas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PagosDocentes_PerfilesDocentes_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "PerfilesDocentes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Academias_TeacherId",
                table: "Academias",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_AcademyId",
                table: "ClasesAgendadas",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_ContentId",
                table: "ClasesAgendadas",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_ParentId",
                table: "ClasesAgendadas",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_RequestId",
                table: "ClasesAgendadas",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_SubjectId",
                table: "ClasesAgendadas",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAgendadas_TeacherId",
                table: "ClasesAgendadas",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_ParentId",
                table: "Contenidos",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Contenidos_TeacherId",
                table: "Contenidos",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_MateriaId",
                table: "DocentesMaterias",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_TeacherId",
                table: "DocentesMaterias",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Hijos_ParentId",
                table: "Hijos",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_ReceiverId",
                table: "Mensajes",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_senderId",
                table: "Mensajes",
                column: "senderId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDocentes_ClaseAgendadaId",
                table: "PagosDocentes",
                column: "ClaseAgendadaId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDocentes_TeacherId",
                table: "PagosDocentes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesPadres_HijoId",
                table: "PerfilesPadres",
                column: "HijoId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesPadres_UserId",
                table: "PerfilesPadres",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_AuthorId",
                table: "Resenas",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_TeacherId",
                table: "Resenas",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesClases_AcademyId",
                table: "SolicitudesClases",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesClases_DocenteId",
                table: "SolicitudesClases",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesClases_MateriaId",
                table: "SolicitudesClases",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesClases_parentId",
                table: "SolicitudesClases",
                column: "parentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocentesMaterias");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "PagosDocentes");

            migrationBuilder.DropTable(
                name: "Resenas");

            migrationBuilder.DropTable(
                name: "ClasesAgendadas");

            migrationBuilder.DropTable(
                name: "Contenidos");

            migrationBuilder.DropTable(
                name: "SolicitudesClases");

            migrationBuilder.DropTable(
                name: "Academias");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "PerfilesDocentes");

            migrationBuilder.DropTable(
                name: "PerfilesPadres");

            migrationBuilder.DropTable(
                name: "Hijos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
