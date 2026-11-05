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
public class ClaseAgendadaController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClaseAgendadaController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/ClaseAgendada/docente/{docenteId}?status=SCHEDULED (solo DOCENTE ve las suyas)
    [Authorize(Roles = "TEACHER")]
    [HttpGet("docente/{docenteId:int}")]
    public async Task<ActionResult<List<ClaseAgendadaDTO>>> GetByDocente(
        int docenteId,
        [FromQuery] string? status = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.Id == docenteId);

        if (perfil == null)
            return NotFound("No se encontró el perfil del docente.");

        if (perfil.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés ver clases de otro docente.");

        var query = _context.ClasesAgendadas
            .Include(c => c.Parent).ThenInclude(p => p.User)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Subject)
            .Include(c => c.Academy)
            .Where(c => c.TeacherId == docenteId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ClaseAgendada.Stat>(status, true, out var statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }
            else
            {
                return BadRequest("Estado inválido. Valores permitidos: SCHEDULED, COMPLETED, CANCELED, NO_SHOW, REFUNDED");
            }
        }

        var clases = await query
            .OrderBy(c => c.ScheduledAt)
            .ToListAsync();

        var result = clases.Select(MapearDTO).ToList();
        return Ok(result);
    }

    // GET: api/ClaseAgendada/padre/{padreId}?status=SCHEDULED (solo PADRE ve las suyas)
    [Authorize(Roles = "PARENT")]
    [HttpGet("padre/{padreId:int}")]
    public async Task<ActionResult<List<ClaseAgendadaDTO>>> GetByPadre(
        int padreId,
        [FromQuery] string? status = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesPadres
            .FirstOrDefaultAsync(p => p.Id == padreId);

        if (perfil == null)
            return NotFound("No se encontró el perfil del padre.");

        if (perfil.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés ver clases de otro padre.");

        var query = _context.ClasesAgendadas
            .Include(c => c.Parent).ThenInclude(p => p.User)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Subject)
            .Include(c => c.Academy)
            .Where(c => c.ParentId == padreId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ClaseAgendada.Stat>(status, true, out var statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }
            else
            {
                return BadRequest("Estado inválido. Valores permitidos: SCHEDULED, COMPLETED, CANCELED, NO_SHOW, REFUNDED");
            }
        }

        var clases = await query
            .OrderBy(c => c.ScheduledAt)
            .ToListAsync();

        var result = clases.Select(MapearDTO).ToList();
        return Ok(result);
    }

    // GET: api/ClaseAgendada/{id} (docente o padre propietario)
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaseAgendadaDTO>> GetById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var clase = await _context.ClasesAgendadas
            .Include(c => c.Parent).ThenInclude(p => p.User)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Subject)
            .Include(c => c.Academy)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clase == null)
            return NotFound("No se encontró la clase.");

        // Verify ownership: teacher or parent must match authenticated user
        if (clase.Teacher.UserId != userId && clase.Parent.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés ver esta clase.");

        return Ok(MapearDTO(clase));
    }

    // PUT: api/ClaseAgendada/{id}/estado (solo DOCENTE propietario)
    [Authorize(Roles = "TEACHER")]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<ClaseAgendadaDTO>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoClaseDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var clase = await _context.ClasesAgendadas
            .Include(c => c.Parent).ThenInclude(p => p.User)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Subject)
            .Include(c => c.Academy)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clase == null)
            return NotFound("No se encontró la clase.");

        if (clase.Teacher.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés modificar clases de otro docente.");

        if (!Enum.TryParse<ClaseAgendada.Stat>(dto.Status, true, out var nuevoEstado))
            return BadRequest("Estado inválido. Valores permitidos: SCHEDULED, COMPLETED, CANCELED, NO_SHOW, REFUNDED");

        // Validate allowed transitions
        if (!TransicionValida(clase.Status, nuevoEstado))
            return BadRequest($"No se puede pasar de {clase.Status} a {nuevoEstado}.");

        clase.Status = nuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(MapearDTO(clase));
    }

    private static bool TransicionValida(ClaseAgendada.Stat actual, ClaseAgendada.Stat nuevo)
    {
        return actual switch
        {
            ClaseAgendada.Stat.SCHEDULED =>
                nuevo == ClaseAgendada.Stat.COMPLETED ||
                nuevo == ClaseAgendada.Stat.CANCELED ||
                nuevo == ClaseAgendada.Stat.NO_SHOW,
            ClaseAgendada.Stat.COMPLETED =>
                nuevo == ClaseAgendada.Stat.REFUNDED,
            _ => false // CANCELED, NO_SHOW, REFUNDED are final states
        };
    }

    private static ClaseAgendadaDTO MapearDTO(ClaseAgendada c) => new()
    {
        Id = c.Id,
        ScheduledAt = c.ScheduledAt,
        Duration = c.Duration,
        Status = c.Status.ToString(),
        Price = c.Price,
        Location = c.Location,
        RequestId = c.RequestId,
        ParentId = c.ParentId,
        ParentName = c.Parent?.User?.Name ?? "",
        TeacherId = c.TeacherId,
        TeacherName = c.Teacher?.User?.Name ?? "",
        SubjectId = c.SubjectId,
        SubjectName = c.Subject?.Name ?? "",
        AcademyId = c.AcademyId,
        AcademyName = c.Academy?.Name ?? ""
    };
}
