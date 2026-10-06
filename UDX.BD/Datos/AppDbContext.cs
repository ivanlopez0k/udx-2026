using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using UDX.BD.Datos.Entity;

namespace UDX.BD.Datos
{
    public class AppDbContext : DbContext
    {
        public DbSet<Academia> Academias { get; set; }
        public DbSet<ClaseAgendada> ClasesAgendadas { get; set; }
        public DbSet<Contenido> Contenidos { get; set; }
        public DbSet<DocenteMateria> DocentesMaterias { get; set; }
        public DbSet<Hijo> Hijos { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<PagoDocente> PagosDocentes { get; set; }
        public DbSet<PerfilDocente> PerfilesDocentes { get; set; }
        public DbSet<PerfilPadre> PerfilesPadres { get; set; }
        public DbSet<Resena> Resenas { get; set; }
        public DbSet<SolicitudClase> SolicitudesClases { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resena>()
                .HasOne(r => r.Author)
                .WithMany()
                .HasForeignKey(r => r.authorResena);

            modelBuilder.Entity<Resena>()
                .HasOne(r => r.Teacher)
                .WithMany()
                .HasForeignKey(r => r.TeacherId);

            foreach (var foreignKey in modelBuilder.Model
              .GetEntityTypes()
              .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            modelBuilder.Entity<Materia>(entity =>
            {
                entity.Property(m => m.Name).HasMaxLength(200);
                entity.Property(m => m.Grade).HasMaxLength(200);
                entity.HasIndex(m => new { m.Name, m.Grade }).IsUnique();
            });

            modelBuilder.Entity<Academia>().HasIndex(a => a.Code).IsUnique();
        }


    }
}
