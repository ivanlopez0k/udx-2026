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
    public async Task<ActionResult<BusquedaDocenteResponseDTO>> Buscar(
        [FromQuery] string? materia,
        [FromQuery] string? ubicacion,
        [FromQuery] decimal? minPrecio,
        [FromQuery] decimal? maxPrecio,
        [FromQuery] float? minRating,
        [FromQuery] float? maxRating,
        [FromQuery] bool? soloVerificados,
        [FromQuery] string? disponibilidad,
        [FromQuery] string? orden,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);
        if (page < 1) page = 1;

        var query = _context.PerfilesDocentes
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(materia))
        {
            query = query.Where(p => p.DocentesMaterias.Any(dm =>
                dm.Materia.Name.ToLower().Contains(materia.ToLower())));
        }

        if (!string.IsNullOrEmpty(ubicacion))
        {
            query = query.Where(p => p.Location.ToLower().Contains(ubicacion.ToLower()));
        }

        if (minPrecio.HasValue)
        {
            query = query.Where(p => p.HourlyRate >= minPrecio.Value);
        }

        if (maxPrecio.HasValue)
        {
            query = query.Where(p => p.HourlyRate <= maxPrecio.Value);
        }

        if (minRating.HasValue)
        {
            query = query.Where(p => p.RatingAvg >= minRating.Value);
        }

        if (maxRating.HasValue)
        {
            query = query.Where(p => p.RatingAvg <= maxRating.Value);
        }

        if (soloVerificados == true)
        {
            query = query.Where(p => p.Verified);
        }

        if (!string.IsNullOrEmpty(disponibilidad))
        {
            query = query.Where(p => p.Availability.ToLower().Contains(disponibilidad.ToLower()));
        }

        query = orden?.ToLower() switch
        {
            "precio" => query.OrderBy(p => p.HourlyRate),
            "nombre" => query.OrderBy(p => p.User.Name),
            _ => query.OrderByDescending(p => p.RatingAvg)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new DocenteDTO
            {
                Id = p.Id,
                Nombre = p.User.Name,
                Location = p.Location,
                HourlyRate = p.HourlyRate,
                RatingAvg = p.RatingAvg,
                RatingCount = p.RatinCount,
                Verified = p.Verified,
                Availability = p.Availability,
                Materias = p.DocentesMaterias.Select(dm => dm.Materia.Name).ToList()
            })
            .ToListAsync();

        return Ok(new BusquedaDocenteResponseDTO
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocenteDetalleDTO>> GetById(int id)
    {
        var perfil = await _context.PerfilesDocentes
            .Include(p => p.User)
            .Include(p => p.DocentesMaterias)
                .ThenInclude(dm => dm.Materia)
            .Include(p => p.DocentesAcademias)
                .ThenInclude(da => da.Academy)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (perfil == null)
            return NotFound();

        var resultado = new DocenteDetalleDTO
        {
            Id = perfil.Id,
            Nombre = perfil.User?.Name,
            Email = perfil.User?.Email,
            Phone = perfil.User?.Phone,
            Credentials = perfil.Credentials,
            HourlyRate = perfil.HourlyRate,
            Location = perfil.Location,
            RatingAvg = perfil.RatingAvg,
            RatingCount = perfil.RatinCount,
            Verified = perfil.Verified,
            Availability = perfil.Availability,
            // Solo membresías activas en academias oficiales activas: la personal nunca se muestra
            Academias = perfil.DocentesAcademias?
                .Where(da => da.IsActive &&
                             da.Academy != null &&
                             da.Academy.IsActive &&
                             !da.Academy.IsPersonal)
                .Select(da => new AcademiaResumenDTO
                {
                    Id = da.Academy.Id,
                    Name = da.Academy.Name
                }).ToList() ?? new List<AcademiaResumenDTO>(),
            Materias = perfil.DocentesMaterias?
                .Select(dm => new MateriaDetalleDTO
                {
                    Name = dm.Materia?.Name,
                    Grade = dm.Materia?.Grade,
                    Category = dm.Materia?.Category,
                    Level = dm.Level
                }).ToList() ?? new List<MateriaDetalleDTO>()
        };

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

    [Authorize(Roles = "TEACHER")]
    [HttpDelete("cuenta")]
    public async Task<IActionResult> EliminarCuenta()
    {
        var userId = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null)
            return NotFound("No se encontró el usuario.");

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil != null)
        {
            var membresias = await _context.DocentesAcademias
                .Where(da => da.TeacherId == perfil.Id)
                .ToListAsync();

            _context.DocentesAcademias.RemoveRange(membresias);
            _context.PerfilesDocentes.Remove(perfil);
        }

        // Solo se borra la academia personal; las oficiales donde era responsable quedan sin responsable
        var academias = await _context.Academias
            .Where(a => a.ResponsableId == userId)
            .ToListAsync();

        foreach (var academia in academias)
        {
            if (academia.IsPersonal)
                _context.Academias.Remove(academia);
            else
                academia.ResponsableId = null;
        }

        _context.Usuarios.Remove(usuario);

        await _context.SaveChangesAsync();

        return Ok("Cuenta eliminada correctamente.");
    }
}
