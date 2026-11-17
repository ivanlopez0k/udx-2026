using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Shared.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResenaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResenaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Resena/docente/{teacherId}
        // Devuelve el listado de reseñas públicas de un docente ordenadas cronológicamente
        [HttpGet("docente/{teacherId:int}")]
        public async Task<ActionResult<List<ResenaDetalleDTO>>> GetResenasDocente(int teacherId)
        {
            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.Id == teacherId || p.UserId == teacherId);

            if (perfil == null)
                return NotFound("No se encontró el docente especificado.");

            var resenas = await _context.Resenas
                .Where(r => r.TeacherId == perfil.Id)
                .Include(r => r.Author)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ResenaDetalleDTO
                {
                    Id = r.Id,
                    TeacherId = r.TeacherId,
                    AutorNombre = r.Author != null && !string.IsNullOrWhiteSpace(r.Author.Name) 
                        ? r.Author.Name 
                        : "Familia UdX",
                    Calificacion = r.Rating,
                    Comentario = r.Comment,
                    Fecha = r.CreatedAt
                })
                .ToListAsync();

            return Ok(resenas);
        }

        // GET: api/Resena/docente/{teacherId}/puede-opinar
        // Valida estrictamente si el usuario actual (PARENT) tiene derecho a dejar una reseña
        [Authorize(Roles = "PARENT")]
        [HttpGet("docente/{teacherId:int}/puede-opinar")]
        public async Task<ActionResult<PuedeOpinarDTO>> PuedeOpinar(int teacherId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var perfilDocente = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.Id == teacherId || p.UserId == teacherId);

            if (perfilDocente == null)
                return NotFound("No se encontró el docente especificado.");

            var perfilPadre = await _context.PerfilesPadres
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (perfilPadre == null)
            {
                return Ok(new PuedeOpinarDTO
                {
                    PuedeOpinar = false,
                    Motivo = "Debés contar con un perfil familiar para poder calificar docentes."
                });
            }

            // 1. Verificar si ya opinó sobre este docente (evita duplicados)
            var yaOpino = await _context.Resenas
                .AnyAsync(r => r.TeacherId == perfilDocente.Id && r.authorResena == userId);

            if (yaOpino)
            {
                return Ok(new PuedeOpinarDTO
                {
                    PuedeOpinar = false,
                    YaOpino = true,
                    Motivo = "Ya dejaste una opinión para este docente."
                });
            }

            // 2. Validación estricta: debe tener una clase agendada o solicitud aceptada con este docente
            var tieneClaseAgendada = await _context.ClasesAgendadas
                .AnyAsync(c => c.ParentId == perfilPadre.Id && c.TeacherId == perfilDocente.Id);

            var tieneSolicitudAceptada = await _context.SolicitudesClases
                .AnyAsync(s => s.ParentId == perfilPadre.Id && 
                               s.DocenteId == perfilDocente.Id && 
                               s.Status == SolicitudClase.Stat.ACCEPTED);

            if (!tieneClaseAgendada && !tieneSolicitudAceptada)
            {
                return Ok(new PuedeOpinarDTO
                {
                    PuedeOpinar = false,
                    YaOpino = false,
                    Motivo = "Solo las familias que hayan coordinado o tomado una clase con este docente pueden calificarlo."
                });
            }

            return Ok(new PuedeOpinarDTO
            {
                PuedeOpinar = true,
                YaOpino = false,
                Motivo = string.Empty
            });
        }

        // POST: api/Resena
        // Guarda la reseña y recalcula automáticamente el promedio y conteo del docente
        [Authorize(Roles = "PARENT")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ResenaDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var perfilDocente = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.Id == dto.TeacherId || p.UserId == dto.TeacherId);

            if (perfilDocente == null)
                return NotFound("No se encontró el docente especificado.");

            var perfilPadre = await _context.PerfilesPadres
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (perfilPadre == null)
                return Forbid("No se encontró el perfil de padre para el usuario actual.");

            // 1. Evitar duplicación de reseñas
            var yaOpino = await _context.Resenas
                .AnyAsync(r => r.TeacherId == perfilDocente.Id && r.authorResena == userId);

            if (yaOpino)
                return BadRequest("Ya dejaste una reseña para este docente.");

            // 2. Validación estricta de negocio: debe existir clase previa o solicitud aceptada
            var tieneClaseAgendada = await _context.ClasesAgendadas
                .AnyAsync(c => c.ParentId == perfilPadre.Id && c.TeacherId == perfilDocente.Id);

            var tieneSolicitudAceptada = await _context.SolicitudesClases
                .AnyAsync(s => s.ParentId == perfilPadre.Id && 
                               s.DocenteId == perfilDocente.Id && 
                               s.Status == SolicitudClase.Stat.ACCEPTED);

            if (!tieneClaseAgendada && !tieneSolicitudAceptada)
                return BadRequest("Solo podés calificar a docentes con los que hayas contratado o coordinado una clase.");

            // 3. Crear y guardar la entidad
            var entidad = new Resena
            {
                TeacherId = perfilDocente.Id,
                authorResena = userId,
                Rating = dto.Calificacion,
                Comment = dto.Comentario.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Resenas.Add(entidad);
            await _context.SaveChangesAsync();

            // 4. Recalcular promedio y cantidad de opiniones de forma atómica
            var todasResenas = await _context.Resenas
                .Where(r => r.TeacherId == perfilDocente.Id)
                .ToListAsync();

            int cantidad = todasResenas.Count;
            float nuevoPromedio = cantidad > 0 
                ? (float)Math.Round(todasResenas.Average(r => r.Rating), 1) 
                : 0f;

            perfilDocente.RatingAvg = nuevoPromedio;
            perfilDocente.RatinCount = cantidad;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Id = entidad.Id,
                NuevoPromedio = nuevoPromedio,
                CantidadResenas = cantidad,
                Mensaje = "¡Tu reseña fue publicada con éxito!"
            });
        }
    }
}
