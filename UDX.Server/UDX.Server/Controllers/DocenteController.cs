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
        var consultaPerfil = _context.PerfilesDocentes
            .Include(p => p.User)
            .Include(p => p.DocentesMaterias)
                .ThenInclude(dm => dm.Materia)
            .Include(p => p.Academy);

        var perfil = await consultaPerfil
            .FirstOrDefaultAsync(p => p.Id == id);

        perfil ??= await consultaPerfil
            .FirstOrDefaultAsync(p => p.UserId == id);

        if (perfil == null)
            return NotFound();

        var resultado = new DocenteDetalleDTO
        {
            Id = perfil.Id,
            Nombre = perfil.User?.Name,
            Email = perfil.User?.Email,
            Phone = perfil.User?.Phone,
            Credentials = perfil.Credentials,
            Location = perfil.Location,
            RatingAvg = perfil.RatingAvg,
            RatingCount = perfil.RatinCount,
            Verified = perfil.Verified,
            Availability = perfil.Availability,
            Academy = perfil.Academy?.Name,
            Foto = perfil.ProfileImage,
            Materias = perfil.DocentesMaterias?
            
                .Select(dm => new MateriaDetalleDTO
                {
                    Name = dm.Materia?.Name,
                    Grade = dm.Materia?.Grade,
                    Category = dm.Materia?.Category,
                    Level = dm.Level,
                    HourlyRate = dm.HourlyRate
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
    [HttpGet("perfil/materias")]
    public async Task<IActionResult> ObtenerMateriasPerfil()
    {
        var userId = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .Include(p => p.DocentesMaterias)
                .ThenInclude(dm => dm.Materia)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var materias = perfil.DocentesMaterias.Select(dm => new
        {
            MateriaId = dm.MateriaId,
            Nombre = dm.Materia.Name,
            Grado = dm.Materia.Grade,
            Nivel = dm.Level,
            PrecioPorHora = dm.HourlyRate
        }).ToList();

        return Ok(materias);
    }

    [Authorize(Roles = "TEACHER")]
    [HttpPost("perfil/materias")]
    public async Task<IActionResult> AgregarMateriaPerfil(
        AgregarMateriaDocenteDTO dto)
    {
        if (dto.MateriaId <= 0)
            return BadRequest("La materia seleccionada no es válida.");

        if (string.IsNullOrWhiteSpace(dto.Nivel))
            return BadRequest("Tenés que indicar el nivel.");

        if (dto.PrecioPorHora < 0)
            return BadRequest("El precio no puede ser negativo.");

        var userId = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .Include(p => p.DocentesMaterias)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var materia = await _context.Materias
            .FirstOrDefaultAsync(m => m.Id == dto.MateriaId);

        if (materia == null)
            return NotFound("La materia seleccionada no existe.");

        var yaExiste = perfil.DocentesMaterias.Any(
            dm => dm.MateriaId == dto.MateriaId
                  && dm.Level == dto.Nivel);

        if (yaExiste)
            return BadRequest("Ya tenés esa materia asociada a ese nivel.");

        var docenteMateria = new DocenteMateria
        {
            TeacherId = perfil.Id,
            MateriaId = materia.Id,
            Level = dto.Nivel,
            HourlyRate = dto.PrecioPorHora
        };

        _context.Set<DocenteMateria>().Add(docenteMateria);

        await _context.SaveChangesAsync();

        return Ok("Materia agregada correctamente a tu perfil.");
    }

    [Authorize(Roles = "TEACHER")]
    [HttpDelete("perfil/materias/{materiaId:int}")]
    public async Task<IActionResult> EliminarMateriaPerfil(
        int materiaId,
        [FromQuery] string nivel)
    {
        var userId = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var docenteMateria = await _context.DocentesMaterias
            .FirstOrDefaultAsync(dm =>
                dm.TeacherId == perfil.Id &&
                dm.MateriaId == materiaId &&
                dm.Level == nivel);

        if (docenteMateria == null)
            return NotFound("La materia no está asociada a tu perfil.");

        _context.DocentesMaterias.Remove(docenteMateria);
        await _context.SaveChangesAsync();

        return NoContent();
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

    [Authorize(Roles = "TEACHER")]
    [HttpPut("materias/precio")]
    public async Task<IActionResult> ActualizarPrecioMateria(
        ActualizarPrecioMateriaDTO dto)
    {
        if (dto.HourlyRate < 0)
            return BadRequest("El precio no puede ser negativo.");

        var userId = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .Include(p => p.DocentesMaterias)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil docente.");

        var docenteMateria = perfil.DocentesMaterias
            .FirstOrDefault(dm => dm.MateriaId == dto.MateriaId);

        if (docenteMateria == null)
            return NotFound("No tenés esa materia asociada a tu perfil.");

        docenteMateria.HourlyRate = dto.HourlyRate;

        await _context.SaveChangesAsync();

        return Ok("Precio de la materia actualizado correctamente.");
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
            _context.PerfilesDocentes.Remove(perfil);
        }

        var academias = await _context.Academias
            .Where(a => a.TeacherId == userId)
            .ToListAsync();

        if (academias.Any())
        {
            _context.Academias.RemoveRange(academias);
        }

        _context.Usuarios.Remove(usuario);

        await _context.SaveChangesAsync();

        return Ok("Cuenta eliminada correctamente.");
    }
}

    }
}
