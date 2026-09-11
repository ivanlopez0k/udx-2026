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
    [HttpPost("foto")]
    public async Task<IActionResult> SubirFoto(IFormFile foto)
    {
        if (foto == null || foto.Length == 0)
            return BadRequest("No se recibió ninguna foto.");

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "perfiles");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        var extension = Path.GetExtension(foto.FileName);
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
}

