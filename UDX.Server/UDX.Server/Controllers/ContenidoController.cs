using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "TEACHER")]
    public class ContenidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContenidoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubirMaterial([FromForm] ContenidoDTO dto)
        {
            var usuarioId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.UserId == usuarioId);

            if (perfil == null)
            {
                return NotFound("No se encontró el perfil docente.");
            }

            var esDocenteDeLaMateria = await _context.DocentesMaterias
                .AnyAsync(dm =>
                    dm.TeacherId == perfil.Id &&
                    dm.MateriaId == dto.MateriaId);

            if (!esDocenteDeLaMateria)
            {
                return BadRequest("El docente no está asociado a esa materia.");
            }

            if (dto.Archivo == null || dto.Archivo.Length == 0)
            {
                return BadRequest("No se recibió ningún archivo.");
            }
            var carpeta = Path.Combine(
    Directory.GetCurrentDirectory(),
    "wwwroot",
    "uploads",
    "contenidos"
);

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            var extension = Path.GetExtension(dto.Archivo.FileName);
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var ruta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await dto.Archivo.CopyToAsync(stream);
            }
            var hijo = await _context.Hijos
                .FirstOrDefaultAsync(h => h.Id == dto.HijoId);

            if (hijo == null)
            {
                return NotFound("No se encontró el alumno.");
            }

            var contenido = new Contenido
            {
                Title = dto.Title,
                Text = "",
                CreatedAt = DateTime.UtcNow,
                TeacherId = usuarioId,
                ParentId = hijo.ParentId,
                FilePath = $"/uploads/contenidos/{nombreArchivo}",
                MateriaId = dto.MateriaId,
                HijoId = dto.HijoId
            };
            await _context.Contenidos.AddAsync(contenido);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}