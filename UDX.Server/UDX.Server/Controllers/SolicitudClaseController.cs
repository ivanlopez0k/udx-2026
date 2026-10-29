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
public class SolicitudClaseController : ControllerBase
{
    private readonly AppDbContext _context;

    public SolicitudClaseController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/SolicitudClase (solo PADRE puede crear)
    [Authorize(Roles = "PARENT")]
    [HttpPost]
    public async Task<ActionResult<SolicitudClaseDTO>> Crear([FromBody] CrearSolicitudDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Validate parent exists and matches authenticated user
        var parent = await _context.PerfilesPadres
            .FirstOrDefaultAsync(p => p.Id == dto.ParentId);

        if (parent == null)
            return BadRequest("No se encontró el perfil del padre.");

        if (parent.UserId != userId)
            return Forbid("No podés crear solicitudes para otro padre.");

        // Validate teacher exists
        var docente = await _context.PerfilesDocentes.FindAsync(dto.DocenteId);
        if (docente == null)
            return BadRequest("No se encontró el perfil del docente.");

        // Validate academy exists
        var academia = await _context.Academias.FindAsync(dto.AcademyId);
        if (academia == null)
            return BadRequest("No se encontró la academia.");

        // Validate materia exists
        var materia = await _context.Materias.FindAsync(dto.MateriaId);
        if (materia == null)
            return BadRequest("No se encontró la materia.");

        // Parse modalidad
        if (!Enum.TryParse<ModalidadClase>(dto.Modalidad, true, out var modalidad))
            return BadRequest("Modalidad inválida. Valores permitidos: ONLINE, PRESENCIAL");

        var solicitud = new SolicitudClase
        {
            Status = SolicitudClase.Stat.PENDING,
            RequestedAt = DateTime.UtcNow,
            Schedule = dto.Schedule,
            Duration = dto.Duration,
            Notes = dto.Notes,
            Message = dto.Message,
            Modalidad = modalidad,
            ParentId = dto.ParentId,
            DocenteId = dto.DocenteId,
            AcademyId = dto.AcademyId,
            MateriaId = dto.MateriaId
        };

        _context.SolicitudesClases.Add(solicitud);
        await _context.SaveChangesAsync();

        var result = await ObtenerSolicitudDTO(solicitud.Id);
        return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, result);
    }

    // GET: api/SolicitudClase/docente/{docenteId}?status=PENDING (solo DOCENTE puede ver las suyas)
    [Authorize(Roles = "TEACHER")]
    [HttpGet("docente/{docenteId:int}")]
    public async Task<ActionResult<List<SolicitudClaseDTO>>> GetByDocente(
        int docenteId,
        [FromQuery] string? status = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .FirstOrDefaultAsync(p => p.Id == docenteId);

        if (perfil == null)
            return NotFound("No se encontró el perfil del docente.");

        if (perfil.UserId != userId)
            return Forbid("No podés ver solicitudes de otro docente.");

        var query = _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .Where(s => s.DocenteId == docenteId)
            .AsQueryable();

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SolicitudClase.Stat>(status, true, out var statusFilter))
            {
                query = query.Where(s => s.Status == statusFilter);
            }
            else
            {
                return BadRequest("Estado inválido. Valores permitidos: PENDING, ACCEPTED, DECLINED, EXPIRED");
            }
        }

        var solicitudes = await query
            .OrderByDescending(s => s.RequestedAt)
            .ToListAsync();

        var result = solicitudes.Select(s => new SolicitudClaseDTO
        {
            Id = s.Id,
            Status = s.Status.ToString(),
            RequestedAt = s.RequestedAt,
            Schedule = s.Schedule,
            Duration = s.Duration,
            Notes = s.Notes,
            Message = s.Message,
            Modalidad = s.Modalidad.ToString(),
            ParentId = s.ParentId,
            ParentName = s.Parent?.User?.Name ?? "",
            DocenteId = s.DocenteId,
            DocenteName = s.Docente?.User?.Name ?? "",
            AcademyId = s.AcademyId,
            AcademyName = s.Academy?.Name ?? "",
            MateriaId = s.MateriaId,
            MateriaName = s.Materia?.Name ?? ""
        }).ToList();

        return Ok(result);
    }

    // GET: api/SolicitudClase/{id} (cualquier usuario autenticado puede ver)
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudClaseDTO>> GetById(int id)
    {
        var dto = await ObtenerSolicitudDTO(id);
        if (dto == null)
            return NotFound();

        return Ok(dto);
    }

    // PUT: api/SolicitudClase/{id}/estado (solo DOCENTE puede aceptar/rechazar)
    [Authorize(Roles = "TEACHER")]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<SolicitudClaseDTO>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoSolicitudDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var solicitud = await _context.SolicitudesClases
            .Include(s => s.Parent)
            .Include(s => s.Docente)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound("No se encontró la solicitud.");

        // Verify the authenticated teacher owns this solicitud
        if (solicitud.Docente.UserId != userId)
            return Forbid("No podés modificar solicitudes de otro docente.");

        if (solicitud.Status != SolicitudClase.Stat.PENDING)
            return BadRequest("Solo se pueden modificar solicitudes en estado PENDING.");

        if (!Enum.TryParse<SolicitudClase.Stat>(dto.Status, true, out var nuevoEstado))
            return BadRequest("Estado inválido. Valores permitidos: ACCEPTED, DECLINED");

        solicitud.Status = nuevoEstado;

        // If accepted, create ClaseAgendada automatically
        if (nuevoEstado == SolicitudClase.Stat.ACCEPTED)
        {
            var clase = new ClaseAgendada
            {
                ScheduledAt = solicitud.Schedule,
                Duration = solicitud.Duration,
                Status = ClaseAgendada.Stat.SCHEDULED,
                Price = 0,
                Location = "",
                RequestId = solicitud.Id,
                ParentId = solicitud.ParentId,
                TeacherId = solicitud.DocenteId,
                SubjectId = solicitud.MateriaId,
                AcademyId = solicitud.AcademyId,
                ContentId = null
            };

            _context.ClasesAgendadas.Add(clase);
        }

        await _context.SaveChangesAsync();

        var result = new SolicitudClaseDTO
        {
            Id = solicitud.Id,
            Status = solicitud.Status.ToString(),
            RequestedAt = solicitud.RequestedAt,
            Schedule = solicitud.Schedule,
            Duration = solicitud.Duration,
            Notes = solicitud.Notes,
            Message = solicitud.Message,
            Modalidad = solicitud.Modalidad.ToString(),
            ParentId = solicitud.ParentId,
            ParentName = solicitud.Parent?.User?.Name ?? "",
            DocenteId = solicitud.DocenteId,
            DocenteName = solicitud.Docente?.User?.Name ?? "",
            AcademyId = solicitud.AcademyId,
            AcademyName = solicitud.Academy?.Name ?? "",
            MateriaId = solicitud.MateriaId,
            MateriaName = solicitud.Materia?.Name ?? ""
        };

        return Ok(result);
    }

    private async Task<SolicitudClaseDTO?> ObtenerSolicitudDTO(int id)
    {
        var s = await _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null) return null;

        return new SolicitudClaseDTO
        {
            Id = s.Id,
            Status = s.Status.ToString(),
            RequestedAt = s.RequestedAt,
            Schedule = s.Schedule,
            Duration = s.Duration,
            Notes = s.Notes,
            Message = s.Message,
            Modalidad = s.Modalidad.ToString(),
            ParentId = s.ParentId,
            ParentName = s.Parent?.User?.Name ?? "",
            DocenteId = s.DocenteId,
            DocenteName = s.Docente?.User?.Name ?? "",
            AcademyId = s.AcademyId,
            AcademyName = s.Academy?.Name ?? "",
            MateriaId = s.MateriaId,
            MateriaName = s.Materia?.Name ?? ""
        };
    }
}
