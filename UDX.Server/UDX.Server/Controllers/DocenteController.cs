using Microsoft.AspNetCore.Mvc;
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
            .Include(p => p.Academy)
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
            Academy = perfil.Academy?.Name,
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
}
