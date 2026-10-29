using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UDX.BD.Datos;
using UDX.Shared.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "PARENT")]
    public class PadreController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PadreController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("perfil")]
        public IActionResult ObtenerPerfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var perfil = _context.PerfilesPadres
                .Include(p => p.User)
                .Include(p => p.Hijo)
                .FirstOrDefault(p => p.UserId == id);

            if (perfil == null)
                return NotFound("No se encontró el perfil del padre.");

            return Ok(new
            {
                Nombre = perfil.User.Name,
                Email = perfil.User.Email,
                Telefono = perfil.User.Phone,
                Ciudad = perfil.Location,
                Hijo = perfil.Hijo.Name,
                Grado = perfil.Hijo.Grade,
                FotoPerfil = perfil.User.FotoPerfil
            });
        }

        [HttpPut("perfil")]
        public IActionResult ActualizarPerfil(PerfilPadreDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var perfil = _context.PerfilesPadres
                .Include(p => p.User)
                .Include(p => p.Hijo)
                .FirstOrDefault(p => p.UserId == id);

            if (perfil == null)
                return NotFound("No se encontró el perfil del padre.");

            perfil.User.Name = dto.Nombre;
            perfil.User.Phone = dto.Telefono;
            perfil.Location = dto.Ciudad;
            perfil.Hijo.Name = dto.Hijo;
            perfil.Hijo.Grade = dto.Grado;

            _context.SaveChanges();

            return Ok();
        }

        [HttpPost("foto")]
        public async Task<IActionResult> SubirFoto(IFormFile foto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
                return NotFound("No se encontró el usuario.");

            if (foto == null || foto.Length == 0)
                return BadRequest("No se recibió ninguna foto.");

            var carpeta = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "perfiles");

            Directory.CreateDirectory(carpeta);

            var extension = Path.GetExtension(foto.FileName);

            var nombreArchivo = $"usuario_{id}{extension}";

            var ruta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            usuario.FotoPerfil = $"/uploads/perfiles/{nombreArchivo}";

            _context.SaveChanges();

            return Ok(new
            {
                FotoPerfil = usuario.FotoPerfil
            });
        }


        [HttpDelete("cuenta")]
        public IActionResult EliminarCuenta()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
                return NotFound("No se encontró el usuario.");

            var perfil = _context.PerfilesPadres
                .FirstOrDefault(p => p.UserId == id);

            if (perfil != null)
            {
                _context.PerfilesPadres.Remove(perfil);
            }

            var hijo = _context.Hijos
                .FirstOrDefault(h => h.ParentId == id);

            if (hijo != null)
            {
                _context.Hijos.Remove(hijo);
            }

            _context.Usuarios.Remove(usuario);

            _context.SaveChanges();

            return Ok("Cuenta eliminada correctamente.");
        }
    }
}