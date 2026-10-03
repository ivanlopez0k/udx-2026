using Microsoft.AspNetCore.Mvc;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;

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

            Usuario usuario = new Usuario
            {
                Email = email,
                Password = dto.Password,
                Name = dto.Name,
                Phone = dto.Phone,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);

            _context.SaveChanges();

            if (usuario.Role == Usuario.Rol.TEACHER)
            {
                PerfilDocente perfil = new PerfilDocente
                {
                    UserId = usuario.Id,
                    Credentials = "",
                    HourlyRate = 0,
                    Location = "",
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = "",
                    Verified = false
                };

                _context.PerfilesDocentes.Add(perfil);
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

            if (usuario.Role == Usuario.Rol.TEACHER)
            {
                PerfilDocente perfil = new PerfilDocente
                {
                    UserId = usuario.Id,
                    Credentials = "",
                    HourlyRate = 0,
                    Location = "",
                    RatingAvg = 0,
                    RatinCount = 0,
                    Availability = "",
                    Verified = false
                };

                _context.PerfilesDocentes.Add(perfil);
                _context.SaveChanges();
            }

            return Ok(usuario);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO dto)
        {
            var usuario = _context.Usuarios
                .FirstOrDefault(u =>
                    u.Email == dto.Email &&
                    u.Password == dto.Password);

            if (usuario == null)
            {
                return Unauthorized("Email o contraseña incorrectos.");
            }

            var token = GenerateToken(usuario);

            return Ok(new
            {
                Token = token,
                usuario.Email,
                usuario.Role
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