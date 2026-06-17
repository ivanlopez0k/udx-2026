using Microsoft.AspNetCore.Mvc;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;
using System.Linq;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDTO dto)
        {
            bool existe = _context.Usuarios
                .Any(u => u.Email == dto.Email);

            if (existe)
            {
                return BadRequest("Ya existe un usuario con ese email.");
            }

            Usuario usuario = new Usuario
            {
                Email = dto.Email,
                Password = dto.Password,
                Name = dto.Name,
                Phone = dto.Phone,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);

            _context.SaveChanges();

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

            return Ok(new
            {
                usuario.Id,
                usuario.Email,
                usuario.Name,
                usuario.Role
            });
        }
    }
}