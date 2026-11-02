using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            bool existe = _context.Usuarios
                .Any(u => u.Email.ToLower() == email);

            if (existe)
            {
                return BadRequest("Ya existe un usuario con ese email.");
            }

            // ADMIN no se puede registrar: valores no permitidos o inválidos pasan a PARENT.
            var role = dto.Role == Usuario.Rol.PARENT ||
                       dto.Role == Usuario.Rol.TEACHER ||
                       dto.Role == Usuario.Rol.ACADEMY
                ? dto.Role
                : Usuario.Rol.PARENT;

            Academia? academiaOficial = null;

            // Validar código de academia para docentes (nadie se une a una academia personal).
            if (role == Usuario.Rol.TEACHER &&
                !string.IsNullOrWhiteSpace(dto.AcademyCode))
            {
                var codigo = dto.AcademyCode.Trim().ToUpper();

                academiaOficial = _context.Academias
                    .FirstOrDefault(a => a.Code.ToUpper() == codigo && !a.IsPersonal);

                if (academiaOficial == null)
                {
                    return BadRequest("Código de academia no válido.");
                }
            }

            var usuario = new Usuario
            {
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Name = dto.Name,
                Phone = dto.Phone,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            if (role == Usuario.Rol.TEACHER)
            {
                // Todo docente tiene siempre su academia personal.
                var academiaPersonal = new Academia
                {
                    Name = $"{dto.Name} Academy",
                    Description = "",
                    Code = GenerateAcademyCode(),
                    CreatedAt = DateTime.UtcNow,
                    IsPersonal = true,
                    IsActive = true,
                    ResponsableId = usuario.Id
                };

                _context.Academias.Add(academiaPersonal);
                _context.SaveChanges();

                var perfil = new PerfilDocente
                {
                    UserId = usuario.Id,
                    Credentials = "",
                    HourlyRate = 0,
                    Location = "",
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = "",
                    Verified = false,
                    ProfileImage = ""
                };

                _context.PerfilesDocentes.Add(perfil);
                _context.SaveChanges();

                _context.DocentesAcademias.Add(new DocenteAcademia
                {
                    TeacherId = perfil.Id,
                    AcademyId = academiaPersonal.Id,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });

                if (academiaOficial != null)
                {
                    _context.DocentesAcademias.Add(new DocenteAcademia
                    {
                        TeacherId = perfil.Id,
                        AcademyId = academiaOficial.Id,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    });

                    // Si la academia estaba inactiva (sin miembros), se reactiva al entrar alguien.
                    if (!academiaOficial.IsActive)
                    {
                        academiaOficial.IsActive = true;
                    }
                }

                _context.SaveChanges();
            }
            else if (role == Usuario.Rol.ACADEMY)
            {
                // La cuenta ACADEMY es responsable de una academia oficial; no tiene PerfilDocente.
                var academia = new Academia
                {
                    Name = dto.Name,
                    Description = "",
                    Code = GenerateAcademyCode(),
                    CreatedAt = DateTime.UtcNow,
                    IsPersonal = false,
                    IsActive = true,
                    Email = email,
                    Phone = dto.Phone,
                    ResponsableId = usuario.Id
                };

                _context.Academias.Add(academia);
                _context.SaveChanges();
            }
            else if (role == Usuario.Rol.PARENT)
            {
                var hijo = new Hijo
                {
                    Name = "",
                    Grade = "",
                    ParentId = usuario.Id
                };

                _context.Hijos.Add(hijo);
                _context.SaveChanges();

                var perfil = new PerfilPadre
                {
                    UserId = usuario.Id,
                    HijoId = hijo.Id,
                    Location = "",
                    Verified = false
                };

                _context.PerfilesPadres.Add(perfil);

                _context.SaveChanges();
            }

            return Ok(usuario);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Email.ToLower() == email);

            if (usuario == null)
            {
                return Unauthorized("No existe una cuenta con ese email.");
            }

            bool passwordValida;

            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    usuario.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Las contraseñas antiguas en texto plano no se aceptan.
                passwordValida = false;
            }

            if (!passwordValida)
            {
                return Unauthorized("La contraseña es incorrecta.");
            }

            var token = GenerateToken(usuario);

            return Ok(new
            {
                Token = token,
                usuario.Email,
                usuario.Role,
                usuario.Name
            });
        }

        [Authorize]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("JWT valido");
        }

        [Authorize]
        [HttpGet("claims")]
        public IActionResult Claims()
        {
            return Ok(User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }));
        }

        private string GenerateAcademyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            return new string(
                Enumerable.Range(0, 8)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
        }

        private string GenerateToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.Id.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    usuario.Email),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Role.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }


        [HttpPost("registro-docente")]
        public IActionResult RegistrarDocente(RegistroDocenteDTO dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (_context.Usuarios.Any(u => u.Email.ToLower() == email))
            {
                return BadRequest("Ya existe un usuario con ese correo electrónico.");
            }

            if (dto.Materias == null || dto.Materias.Count == 0)
            {
                return BadRequest("Tenés que seleccionar al menos una materia.");
            }

            if (dto.Materias.Any(m =>
                m.MateriaId <= 0 ||
                string.IsNullOrWhiteSpace(m.Nivel) ||
                m.PrecioPorHora <= 0))
            {
                return BadRequest(
                    "Cada materia debe tener un identificador válido, un nivel y un precio mayor que cero.");
            }

            // Evitar que se repita la misma materia para el mismo nivel.
            bool materiasRepetidas = dto.Materias
                .GroupBy(m => new
                {
                    m.MateriaId,
                    Nivel = m.Nivel.Trim().ToLowerInvariant()
                })
                .Any(g => g.Count() > 1);

            if (materiasRepetidas)
            {
                return BadRequest(
                    "No podés registrar la misma materia dos veces para el mismo nivel.");
            }

            var idsMaterias = dto.Materias
                .Select(m => m.MateriaId)
                .Distinct()
                .ToList();

            var materiasExistentes = _context.Materias
                .Where(m => idsMaterias.Contains(m.Id))
                .Select(m => m.Id)
                .ToList();

            if (materiasExistentes.Count != idsMaterias.Count)
            {
                return BadRequest("Una o más materias no existen.");
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var usuario = new Usuario
                {
                    Name = dto.Name.Trim(),
                    Email = email,
                    Phone = "",
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = Usuario.Rol.TEACHER,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                var academiaPersonal = new Academia
                {
                    Name = $"{usuario.Name} Academy",
                    Description = "",
                    Code = GenerateAcademyCode(),
                    CreatedAt = DateTime.UtcNow,
                    IsPersonal = true,
                    IsActive = true,
                    ResponsableId = usuario.Id
                };

                _context.Academias.Add(academiaPersonal);

                var perfil = new PerfilDocente
                {
                    UserId = usuario.Id,
                    Credentials = "",
                    HourlyRate = 0,
                    Location = dto.Zona.Trim(),
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = "",
                    Verified = false,
                    ProfileImage = ""
                };

                _context.PerfilesDocentes.Add(perfil);
                _context.SaveChanges();

                _context.DocentesAcademias.Add(new DocenteAcademia
                {
                    TeacherId = perfil.Id,
                    AcademyId = academiaPersonal.Id,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });

                foreach (var materia in dto.Materias)
                {
                    _context.DocentesMaterias.Add(new DocenteMateria
                    {
                        TeacherId = perfil.Id,
                        MateriaId = materia.MateriaId,
                        Level = materia.Nivel.Trim(),
                        HourlyRate = materia.PrecioPorHora
                    });
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok(new
                {
                    mensaje = "La cuenta docente se creó correctamente.",
                    usuario.Email,
                    usuario.Name,
                    zona = perfil.Location,
                    materiasRegistradas = dto.Materias.Count
                });
            }
            catch
            {
                transaction.Rollback();
                return StatusCode(
                    500,
                    "No se pudo completar el registro docente. No se guardaron los cambios.");
            }
        }

    }
}