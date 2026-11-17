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
            else
            {
                academia.Code = "SEED0001";
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

            SeedDemo(context);
        }

        // Datos de demo para la expo (primaria en Córdoba). Idempotente: busca antes de crear
        // y nunca pisa lo que ya existe. Contraseña de todos los usuarios: Demo1234!
        public static void SeedDemo(AppDbContext context)
        {
            const string passwordDemo = "Demo1234!";

            // Academia oficial con su cuenta ACADEMY como responsable
            var aprender = ObtenerOCrearUsuario(context, "aprender@udx.com", "Aprender Córdoba", "3514556677", Usuario.Rol.ACADEMY, passwordDemo);

            var academiaAprender = context.Academias
                .FirstOrDefault(a => a.Name == "Academia Aprender Córdoba");

            if (academiaAprender == null)
            {
                academiaAprender = new Academia
                {
                    Name = "Academia Aprender Córdoba",
                    Description = "Apoyo escolar para chicos de 1° a 7° grado con docentes de nivel primario. Clases individuales y en grupos reducidos, presenciales u online.",
                    Code = GenerarCodigoAcademia(),
                    CreatedAt = DateTime.UtcNow,
                    IsPersonal = false,
                    IsActive = true,
                    Location = "Nueva Córdoba, Córdoba",
                    Phone = "3514556677",
                    Email = "aprender@udx.com",
                    PricePerHour = 6000,
                    PricePerCourse = 45000,
                    ResponsableId = aprender.Id
                };

                context.Academias.Add(academiaAprender);
                context.SaveChanges();
            }

            // Docentes de la academia oficial
            var laura = ObtenerOCrearDocente(context,
                ObtenerOCrearUsuario(context, "laura.fernandez@udx.com", "Laura Fernández", "3515123456", Usuario.Rol.TEACHER, passwordDemo),
                location: "Nueva Córdoba, Córdoba",
                credentials: "Profesora de Nivel Primario, 8 años de experiencia en primer ciclo. Especializada en alfabetización inicial y matemática.",
                availability: "Lunes a viernes de 14 a 19 h",
                hourlyRate: 6000);

            var martin = ObtenerOCrearDocente(context,
                ObtenerOCrearUsuario(context, "martin.rodriguez@udx.com", "Martín Rodríguez", "3515234567", Usuario.Rol.TEACHER, passwordDemo),
                location: "General Paz, Córdoba",
                credentials: "Profesor de Nivel Primario y Profesor de Inglés, 6 años de experiencia en segundo ciclo.",
                availability: "Lunes, miércoles y viernes de 15 a 20 h",
                hourlyRate: 6000);

            CrearMembresiaActiva(context, laura.Id, academiaAprender.Id);
            CrearMembresiaActiva(context, martin.Id, academiaAprender.Id);

            var precioAprender = academiaAprender.PricePerHour ?? 0;

            foreach (var nivel in new[] { "1", "2", "3" })
            {
                AsignarMateriaDocente(context, laura.Id, "Matemática", nivel, precioAprender);
                AsignarMateriaDocente(context, laura.Id, "Lengua", nivel, precioAprender);
            }

            foreach (var nivel in new[] { "4", "5", "6", "7" })
            {
                AsignarMateriaDocente(context, martin.Id, "Ciencias Naturales", nivel, precioAprender);
                AsignarMateriaDocente(context, martin.Id, "Inglés", nivel, precioAprender);
            }

            // Docente particular: solo su academia personal
            var usuarioCarolina = ObtenerOCrearUsuario(context, "carolina.sosa@udx.com", "Carolina Sosa", "3515345678", Usuario.Rol.TEACHER, passwordDemo);
            var carolina = ObtenerOCrearDocente(context,
                usuarioCarolina,
                location: "Cerro de las Rosas, Córdoba",
                credentials: "Profesora de Nivel Primario, 10 años de experiencia en segundo ciclo. Preparación para el ingreso al secundario.",
                availability: "Martes y jueves de 14 a 20 h, sábados de 9 a 13 h",
                hourlyRate: 5000,
                pricePerHourPersonal: 5000);

            var academiaCarolina = context.Academias
                .First(a => a.IsPersonal && a.ResponsableId == usuarioCarolina.Id);

            var precioCarolina = academiaCarolina.PricePerHour ?? 0;

            foreach (var nivel in new[] { "4", "5", "6", "7" })
            {
                AsignarMateriaDocente(context, carolina.Id, "Matemática", nivel, precioCarolina);
                AsignarMateriaDocente(context, carolina.Id, "Ciencias Sociales", nivel, precioCarolina);
            }

            // Padres con su hijo
            ObtenerOCrearPadre(context,
                ObtenerOCrearUsuario(context, "ana.gomez@udx.com", "Ana Gómez", "3516456789", Usuario.Rol.PARENT, passwordDemo),
                location: "Córdoba",
                nombreHijo: "Tomás Gómez",
                gradoHijo: "3° Grado Primaria");

            ObtenerOCrearPadre(context,
                ObtenerOCrearUsuario(context, "diego.perez@udx.com", "Diego Pérez", "3516567890", Usuario.Rol.PARENT, passwordDemo),
                location: "Córdoba",
                nombreHijo: "Valentina Pérez",
                gradoHijo: "5° Grado Primaria");
        }

        private static Usuario ObtenerOCrearUsuario(AppDbContext context, string email, string name, string phone, Usuario.Rol role, string password)
        {
            var usuario = context.Usuarios
                .FirstOrDefault(u => u.Email.ToLower() == email);

            if (usuario == null)
            {
                usuario = new Usuario
                {
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role,
                    Name = name,
                    Phone = phone,
                    CreatedAt = DateTime.UtcNow
                };

                context.Usuarios.Add(usuario);
                context.SaveChanges();
            }

            return usuario;
        }

        // Igual que el registro de un docente: academia personal (responsable = el docente),
        // PerfilDocente y membresía activa en la academia personal.
        private static PerfilDocente ObtenerOCrearDocente(AppDbContext context, Usuario usuario, string location, string credentials, string availability, decimal hourlyRate, decimal? pricePerHourPersonal = null)
        {
            var academiaPersonal = context.Academias
                .FirstOrDefault(a => a.IsPersonal && a.ResponsableId == usuario.Id);

            if (academiaPersonal == null)
            {
                academiaPersonal = new Academia
                {
                    Name = $"{usuario.Name} Academy",
                    Description = "",
                    Code = GenerarCodigoAcademia(),
                    CreatedAt = DateTime.UtcNow,
                    IsPersonal = true,
                    IsActive = true,
                    PricePerHour = pricePerHourPersonal,
                    ResponsableId = usuario.Id
                };

                context.Academias.Add(academiaPersonal);
                context.SaveChanges();
            }

            var perfil = context.PerfilesDocentes
                .FirstOrDefault(p => p.UserId == usuario.Id);

            if (perfil == null)
            {
                perfil = new PerfilDocente
                {
                    UserId = usuario.Id,
                    Credentials = credentials,
                    // La búsqueda de docentes muestra y ordena por este campo: va el precio por hora de su academia
                    HourlyRate = hourlyRate,
                    Location = location,
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = availability,
                    Verified = true,
                    ProfileImage = ""
                };

                context.PerfilesDocentes.Add(perfil);
                context.SaveChanges();
            }

            CrearMembresiaActiva(context, perfil.Id, academiaPersonal.Id);

            return perfil;
        }

        // Las materias del catálogo van una por grado ("1" a "7"); el nivel del docente usa el mismo número.
        private static void AsignarMateriaDocente(AppDbContext context, int teacherId, string nombreMateria, string nivel, decimal hourlyRate)
        {
            var materia = context.Materias
                .FirstOrDefault(m => m.Name == nombreMateria && m.Grade == nivel);

            if (materia == null)
            {
                materia = new Materia
                {
                    Name = nombreMateria,
                    Grade = nivel,
                    Category = CategoriaMateria(nombreMateria)
                };

                context.Materias.Add(materia);
                context.SaveChanges();
            }

            var existe = context.DocentesMaterias
                .Any(dm => dm.TeacherId == teacherId && dm.MateriaId == materia.Id && dm.Level == nivel);

            if (!existe)
            {
                context.DocentesMaterias.Add(new DocenteMateria
                {
                    TeacherId = teacherId,
                    MateriaId = materia.Id,
                    Level = nivel,
                    HourlyRate = hourlyRate
                });
                context.SaveChanges();
            }
        }

        // Misma categoría que usa la migración SeedCatalogoMaterias
        private static string CategoriaMateria(string nombre) => nombre switch
        {
            "Matemática" => "Ciencias Exactas",
            "Lengua" => "Lenguas",
            "Ciencias Naturales" => "Ciencias Naturales",
            "Ciencias Sociales" => "Ciencias Sociales",
            _ => "Idiomas",
        };

        // Igual que el registro de un padre: primero el Hijo (ParentId = Id del usuario) y después el PerfilPadre.
        private static void ObtenerOCrearPadre(AppDbContext context, Usuario usuario, string location, string nombreHijo, string gradoHijo)
        {
            if (context.PerfilesPadres.Any(p => p.UserId == usuario.Id))
                return;

            var hijo = new Hijo
            {
                Name = nombreHijo,
                Grade = gradoHijo,
                ParentId = usuario.Id
            };

            context.Hijos.Add(hijo);
            context.SaveChanges();

            context.PerfilesPadres.Add(new PerfilPadre
            {
                UserId = usuario.Id,
                HijoId = hijo.Id,
                Location = location,
                Verified = false
            });
            context.SaveChanges();
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