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
}

