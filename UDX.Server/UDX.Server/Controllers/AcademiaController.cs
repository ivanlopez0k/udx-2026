using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AcademiaController : ControllerBase
{
    private readonly AppDbContext _context;

    public AcademiaController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Academia (solo TEACHER, máximo una creada por docente)
    [Authorize(Roles = "TEACHER")]
    [HttpPost]
    public async Task<ActionResult<AcademiaDTO>> Crear([FromBody] CrearAcademiaDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verificar que el usuario tenga PerfilDocente
        var perfilDocente = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfilDocente == null)
            return BadRequest("No se encontró el perfil de docente.");

        // Máximo una academia no-personal creada por este docente
        var yaCreo = await _context.Academias
            .AnyAsync(a => a.ResponsableId == userId && !a.IsPersonal);

        if (yaCreo)
            return BadRequest("Ya creaste una academia. Solo se permite una por docente.");

        var academia = new Academia
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Code = GenerateAcademyCode(),
            CreatedAt = DateTime.UtcNow,
            IsPersonal = false,
            IsActive = true,
            Location = dto.Location?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            ResponsableId = userId
        };

        _context.Academias.Add(academia);
        await _context.SaveChangesAsync();

        // El creador queda como miembro activo
        var membresia = new DocenteAcademia
        {
            TeacherId = perfilDocente.Id,
            AcademyId = academia.Id,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.DocentesAcademias.Add(membresia);
        await _context.SaveChangesAsync();

        var result = new AcademiaDTO
        {
            Id = academia.Id,
            Name = academia.Name,
            Description = academia.Description,
            Code = academia.Code,
            CreatedAt = academia.CreatedAt,
            Location = academia.Location,
            Phone = academia.Phone,
            Email = academia.Email,
            ResponsableId = academia.ResponsableId,
            IsActive = academia.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = academia.Id }, result);
    }

    // GET: api/Academia/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AcademiaDTO>> GetById(int id)
    {
        var academia = await _context.Academias
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsPersonal);

        if (academia == null)
            return NotFound("Academia no encontrada.");

        return Ok(new AcademiaDTO
        {
            Id = academia.Id,
            Name = academia.Name,
            Description = academia.Description,
            Code = academia.Code,
            CreatedAt = academia.CreatedAt,
            Location = academia.Location,
            Phone = academia.Phone,
            Email = academia.Email,
            ResponsableId = academia.ResponsableId,
            IsActive = academia.IsActive
        });
    }

    private string GenerateAcademyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        return new string(
            Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
    }
}
