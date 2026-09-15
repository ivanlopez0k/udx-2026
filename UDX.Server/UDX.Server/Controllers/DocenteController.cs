using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;
using UDX.BD.Datos;

namespace UDX.Server.Controllers;
[ApiController]
[Route("api/[controller]")]
public class DocenteController : ControllerBase
{
    private readonly AppDbContext _context;
    public DocenteController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<IEnumerable<DocenteDTO>>> Buscar(
        [FromQuery] string? materia,
        [FromQuery] string? ubicacion)
    {
        var query = _context.PerfilesDocentes
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(ubicacion))
        {
            query = query.Where(p => p.Location == ubicacion);
        }

        if (!string.IsNullOrEmpty(materia))
        {
            var teacherIds = _context.DocentesMaterias
                .Where(dm => dm.Materia.Name == materia)
                .Select(dm => dm.TeacherId);

            query = query.Where(p => teacherIds.Contains(p.Id));
        }

        var perfiles = await query.ToListAsync();

        var resultado = perfiles.Select(p => new DocenteDTO
        {
            Id = p.Id,
            Nombre = p.User?.Name,
            Location = p.Location,
            Materias = _context.DocentesMaterias
                .Where(dm => dm.TeacherId == p.Id)
                .Select(dm => dm.Materia.Name)
                .ToList()
        }).ToList();

        return Ok(resultado);
    }

    [Authorize(Roles = "TEACHER")]
    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        return Ok(new
        {
            Id = perfil.Id,
            Nombre = perfil.User.Name,
            Email = perfil.User.Email,
            Telefono = perfil.User.Phone,
            Credenciales = perfil.Credentials,
            PrecioPorHora = perfil.HourlyRate,
            Ubicacion = perfil.Location,
            Calificacion = perfil.RatingAvg,
            CantidadResenas = perfil.RatinCount,
            Disponibilidad = perfil.Availability,
            Verificado = perfil.Verified,
            Foto = perfil.ProfileImage
        });
    }

   
    [Authorize(Roles = "TEACHER")]
    [HttpPost("foto")]
    public async Task<IActionResult> SubirFoto(IFormFile foto)
    {
        if (foto == null || foto.Length == 0)
            return BadRequest("No se recibió ninguna foto.");

        if (foto.Length > 5 * 1024 * 1024)
            return BadRequest("La foto no puede superar los 5 MB.");

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension))
            return BadRequest("Formato de imagen no permitido.");

        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var carpeta = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "perfiles");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

       
        if (!string.IsNullOrEmpty(perfil.ProfileImage))
        {
            var rutaFotoAnterior = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                perfil.ProfileImage
                    .TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(rutaFotoAnterior))
            {
                System.IO.File.Delete(rutaFotoAnterior);
            }
        }

        var nombreArchivo = $"profesor_{userId}{extension}";

        var ruta = Path.Combine(carpeta, nombreArchivo);

        
        using (var stream = new FileStream(ruta, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        perfil.ProfileImage = $"/uploads/perfiles/{nombreArchivo}";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Foto subida correctamente.",
            foto = perfil.ProfileImage
        });
    }
    [Authorize(Roles = "TEACHER")]
    [HttpGet("foto")]
    public async Task<IActionResult> ObtenerFoto()
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        return Ok(new
        {
            foto = perfil.ProfileImage
        });
    }
    [Authorize(Roles = "TEACHER")]
    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarPerfil(
    ActualizarPerfilDocenteDTO dto)
    {
        var userId = int.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        perfil.User.Name = dto.Nombre;
        perfil.User.Phone = dto.Telefono;

        perfil.Credentials = dto.Credenciales;
        perfil.HourlyRate = dto.PrecioPorHora;
        perfil.Location = dto.Ubicacion;
        perfil.Availability = dto.Disponibilidad;

        await _context.SaveChangesAsync();

        return Ok("Perfil actualizado correctamente.");
    }
}

