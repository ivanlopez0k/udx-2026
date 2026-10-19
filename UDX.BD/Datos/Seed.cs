using UDX.BD.Datos;
using UDX.BD.Datos.Entity;

namespace UDX.BD.Datos
{
    public static class Seed
    {
        public static void Inicializar(AppDbContext context)
        {
            var profesor = context.Usuarios
                .FirstOrDefault(u => u.Email == "profesor@test.com");

            if (profesor == null)
            {
                profesor = new Usuario
                {
                    Email = "profesor@test.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = Usuario.Rol.TEACHER,
                    Name = "Profesor de Prueba",
                    Phone = "3511234567",
                    CreatedAt = DateTime.Now
                };

                context.Usuarios.Add(profesor);
                context.SaveChanges();
            }
            else if (!profesor.Password.StartsWith("$2"))
            {
                profesor.Password = BCrypt.Net.BCrypt.HashPassword("123456");
                context.SaveChanges();
            }
            var academia = context.Academias
                .FirstOrDefault(a => a.Name == "Academia de Prueba");

            if (academia == null)
            {
                academia = new Academia
                {
                    Name = "Academia de Prueba",
                    Description = "Academia creada para realizar pruebas",
                    Code = "SEED0001",
                    CreatedAt = DateTime.Now,
                    ResponsableId = profesor.Id,
                    IsPersonal = false,
                    IsActive = true,
                    Location = "Córdoba, Argentina",
                    Phone = "3510000000",
                    Email = "academia@test.com"
                };

                context.Academias.Add(academia);
                context.SaveChanges();
            }
            else if (academia.Location == null && academia.Phone == null && academia.Email == null)
            {
                academia.Location = "Córdoba, Argentina";
                academia.Phone = "3510000000";
                academia.Email = "academia@test.com";
                context.SaveChanges();
            }

            var academiaPersonal = context.Academias
                .FirstOrDefault(a => a.IsPersonal && a.ResponsableId == profesor.Id);

            if (academiaPersonal == null)
            {
                academiaPersonal = new Academia
                {
                    Name = $"{profesor.Name} Academy",
                    Description = "",
                    Code = GenerarCodigoAcademia(),
                    CreatedAt = DateTime.UtcNow,
                    ResponsableId = profesor.Id,
                    IsPersonal = true,
                    IsActive = true
                };

                context.Academias.Add(academiaPersonal);
                context.SaveChanges();
            }

            var perfilDocente = context.PerfilesDocentes
    .FirstOrDefault(p => p.UserId == profesor.Id);

            if (perfilDocente == null)
            {
                perfilDocente = new PerfilDocente
                {
                    UserId = profesor.Id,
                    Credentials = "",
                    HourlyRate = 0,
                    Location = "",
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = "",
                    Verified = false,
                    ProfileImage = ""
                };

                context.PerfilesDocentes.Add(perfilDocente);
                context.SaveChanges();
            }

            CrearMembresiaActiva(context, perfilDocente.Id, academiaPersonal.Id);
            CrearMembresiaActiva(context, perfilDocente.Id, academia.Id);

            var materia = context.Materias
    .FirstOrDefault(m => m.Name == "Matemática" && m.Grade == "1");

            if (materia == null)
            {
                materia = new Materia
                {
                    Name = "Matemática",
                    Grade = "1",
                    Category = "Ciencias Exactas"
                };

                context.Materias.Add(materia);
                context.SaveChanges();
            }
            var docenteMateria = context.DocentesMaterias
    .FirstOrDefault(dm =>
        dm.TeacherId == perfilDocente.Id &&
        dm.MateriaId == materia.Id);

            if (docenteMateria == null)
            {
                docenteMateria = new DocenteMateria
                {
                    TeacherId = perfilDocente.Id,
                    MateriaId = materia.Id,
                    Level = "Inicial"
                };

                context.DocentesMaterias.Add(docenteMateria);
                context.SaveChanges();
            }
            

            var padre = context.Usuarios
                .FirstOrDefault(u => u.Email == "padre@test.com");

            

            if (padre == null)
            {
                padre = new Usuario
                {
                    Email = "padre@test.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = Usuario.Rol.PARENT,
                    Name = "Padre de Prueba",
                    Phone = "3517654321",
                    CreatedAt = DateTime.Now
                };

                context.Usuarios.Add(padre);
                context.SaveChanges();
            }
            else if (!padre.Password.StartsWith("$2"))
            {
                padre.Password = BCrypt.Net.BCrypt.HashPassword("123456");
                context.SaveChanges();
            }

        }

        private static void CrearMembresiaActiva(AppDbContext context, int teacherId, int academyId)
        {
            var membresia = context.DocentesAcademias
                .FirstOrDefault(da => da.TeacherId == teacherId && da.AcademyId == academyId);

            if (membresia == null)
            {
                context.DocentesAcademias.Add(new DocenteAcademia
                {
                    TeacherId = teacherId,
                    AcademyId = academyId,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });
                context.SaveChanges();
            }
            else if (!membresia.IsActive)
            {
                membresia.IsActive = true;
                membresia.LeftAt = null;
                context.SaveChanges();
            }
        }

        private static string GenerarCodigoAcademia()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}