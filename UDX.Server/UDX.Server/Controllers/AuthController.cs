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

            var role = dto.Role == Usuario.Rol.PARENT || dto.Role == Usuario.Rol.TEACHER
                ? dto.Role
                : Usuario.Rol.PARENT;

            // Step 1: Validate academy code BEFORE saving anything
            int? academiaId = null;

            if (role == Usuario.Rol.TEACHER)
            {
                if (!string.IsNullOrEmpty(dto.AcademyCode))
                {
                    var academia = _context.Academias
                        .FirstOrDefault(a => a.Code.ToUpper() == dto.AcademyCode.ToUpper());

                    // Mismo mensaje si no existe o si es personal: nadie se une a una academia personal
                    if (academia == null || academia.IsPersonal)
                        return BadRequest("Código de academia no válido.");

                    academiaId = academia.Id;
                }
            }

            // Step 2: Save user first (to get Id)
            Usuario usuario = new Usuario
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

            // Step 3: Create personal academy + perfil + memberships for TEACHER
            if (role == Usuario.Rol.TEACHER)
            {
                // Every teacher always gets a personal (hidden) academy
                var academiaPersonal = new Academia
                {
                    Name = $"{dto.Name} Academy",
                    Description = "",
                    Code = GenerateAcademyCode(),
                    CreatedAt = DateTime.UtcNow,
                    TeacherId = usuario.Id,
                    IsPersonal = true
                };

                _context.Academias.Add(academiaPersonal);
                _context.SaveChanges();

                PerfilDocente perfil = new PerfilDocente
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
                    Role = DocenteAcademia.AcademyRole.DOCENTE,
                    JoinedAt = DateTime.UtcNow
                });

                // If a valid official academy code was provided, join it too
                if (academiaId.HasValue)
                {
                    _context.DocentesAcademias.Add(new DocenteAcademia
                    {
                        TeacherId = perfil.Id,
                        AcademyId = academiaId.Value,
                        Role = DocenteAcademia.AcademyRole.DOCENTE,
                        JoinedAt = DateTime.UtcNow
                    });
                }

                _context.SaveChanges();
            }
            else if (usuario.Role == Usuario.Rol.PARENT)
            {
                Hijo hijo = new Hijo
                {
                    Name = "",
                    Grade = "",
                    ParentId = usuario.Id
                };

                _context.Hijos.Add(hijo);

                _context.SaveChanges();

                PerfilPadre perfil = new PerfilPadre
                {
                    UserId = usuario.Id,
                    HijoId = hijo.Id,
                    Location = "",
                    Verified = false
                };

                _context.PerfilesPadres.Add(perfil);

                _context.SaveChanges();
            }

            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return BadRequest(
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
                );
            }

          

            return Ok(usuario);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO dto)
        {
            
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Email == dto.Email);

            if (usuario == null)
            {
                return Unauthorized("No existe una cuenta con ese email.");
            }

            bool passwordValida;
            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Contraseña vieja guardada en texto plano (sin hash)
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
            return new string(Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        private string GenerateToken(Usuario usuario)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier,
            usuario.Id.ToString()),

        new Claim(ClaimTypes.Email,
            usuario.Email),

        new Claim(ClaimTypes.Role,
            usuario.Role.ToString())
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

    }
}