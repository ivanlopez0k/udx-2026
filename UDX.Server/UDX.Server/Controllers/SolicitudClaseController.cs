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
    public async Task<ActionResult<SolicitudClaseDTO>> Crear(
        [FromBody] CrearSolicitudDTO dto)
    {
        if (dto.Schedule.UtcDateTime <= DateTime.UtcNow)
            return BadRequest("La fecha de la clase debe ser futura.");

        if (dto.Duration <= 0 || dto.Duration > 4)
            return BadRequest("La duración debe ser de entre 1 y 4 horas.");

        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("El mensaje es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Level))
            return BadRequest("Debés seleccionar un nivel.");

        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var parent = await _context.PerfilesPadres
            .FirstOrDefaultAsync(p => p.Id == dto.ParentId);

        if (parent == null)
            return BadRequest("No se encontró el perfil del padre.");

        if (parent.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés crear solicitudes para otro padre.");

        var docente = await _context.PerfilesDocentes
            .FindAsync(dto.DocenteId);

        if (docente == null)
            return BadRequest("No se encontró el perfil del docente.");

        // El docente debe tener al menos una academia activa.
        var tieneAcademiaActiva = await _context.DocentesAcademias
            .AnyAsync(da =>
                da.TeacherId == dto.DocenteId &&
                da.IsActive &&
                da.Academy != null &&
                da.Academy.IsActive);

        if (!tieneAcademiaActiva)
            return BadRequest("El docente no tiene ninguna academia activa.");

        var materia = await _context.Materias
            .FindAsync(dto.MateriaId);

        if (materia == null)
            return BadRequest("No se encontró la materia.");

        var ofertaMateria = await _context.DocentesMaterias
            .FirstOrDefaultAsync(dm =>
                dm.TeacherId == dto.DocenteId &&
                dm.MateriaId == dto.MateriaId &&
                dm.Level == dto.Level);

        if (ofertaMateria == null)
            return BadRequest("El docente no ofrece esta materia en el nivel seleccionado.");

        if (!Enum.TryParse<ModalidadClase>(
            dto.Modalidad, true, out var modalidad))
        {
            return BadRequest(
                "Modalidad inválida. Valores permitidos: ONLINE, PRESENCIAL");
        }

        var solicitud = new SolicitudClase
        {
            Status = SolicitudClase.Stat.PENDING,
            RequestedAt = DateTime.UtcNow,
            Schedule = dto.Schedule.UtcDateTime,
            Duration = dto.Duration,
            Notes = dto.Notes,
            Message = dto.Message,
            Modalidad = modalidad,
            ParentId = dto.ParentId,
            DocenteId = dto.DocenteId,
            MateriaId = dto.MateriaId,
            Level = dto.Level,

            // La academia se seleccionará cuando el docente acepte.
            AcademyId = null
        };

        _context.SolicitudesClases.Add(solicitud);
        await _context.SaveChangesAsync();

        var result = await ObtenerSolicitudDTO(solicitud.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = solicitud.Id },
            result);
    }

    // GET: api/SolicitudClase/docente/{docenteId}?status=PENDING
    // GET: api/SolicitudClase/docente/{docenteId}?status=PENDING
    [Authorize(Roles = "TEACHER")]
    [HttpGet("docente/{docenteId:int}")]
    public async Task<ActionResult<List<SolicitudClaseDTO>>> GetByDocente(
        int docenteId,
        [FromQuery] string? status = null)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesDocentes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == docenteId);

        if (perfil == null)
            return NotFound("No se encontró el perfil del docente.");

        if (perfil.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés ver solicitudes de otro docente.");

        var query = _context.SolicitudesClases
            .AsNoTracking()
            .Where(s => s.DocenteId == docenteId);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SolicitudClase.Stat>(
                status, true, out var statusFilter))
            {
                query = query.Where(s => s.Status == statusFilter);
            }
            else
            {
                return BadRequest(
                    "Estado inválido. Valores permitidos: PENDING, ACCEPTED, DECLINED, EXPIRED");
            }
        }

        var result = await query
            .OrderByDescending(s => s.RequestedAt)
            .Select(s => new SolicitudClaseDTO
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
                ParentName = s.Parent.User.Name,

                DocenteId = s.DocenteId,
                DocenteName = s.Docente.User.Name,

                AcademyId = s.AcademyId,
                AcademyName = s.Academy != null
                    ? s.Academy.Name
                    : "",

                MateriaId = s.MateriaId,
                MateriaName = s.Materia.Name,

                Level = s.Level
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET: api/SolicitudClase/padre
    [Authorize(Roles = "PARENT")]
    [HttpGet("padre")]
    public async Task<ActionResult<List<SolicitudClaseDTO>>> GetByPadre()
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var perfil = await _context.PerfilesPadres
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil == null)
            return NotFound("No se encontró el perfil del padre.");

        var solicitudes = await _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .Where(s => s.ParentId == perfil.Id)
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
            MateriaName = s.Materia?.Name ?? "",
            Level = s.Level
        }).ToList();

        return Ok(result);
    }

    // GET: api/SolicitudClase/{id}
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudClaseDTO>> GetById(int id)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var solicitud = await _context.SolicitudesClases
            .Include(s => s.Parent)
            .Include(s => s.Docente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound();

        bool esPadre = solicitud.Parent.UserId == userId;
        bool esDocente = solicitud.Docente.UserId == userId;

        if (!esPadre && !esDocente)
            return Forbid();

        var dto = await ObtenerSolicitudDTO(id);

        return Ok(dto);
    }

    // PUT: api/SolicitudClase/{id}/estado
    [Authorize(Roles = "TEACHER")]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<SolicitudClaseDTO>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoSolicitudDTO dto)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var solicitud = await _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound("No se encontró la solicitud.");

        if (solicitud.Docente.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "No podés modificar solicitudes de otro docente.");

        if (solicitud.Status != SolicitudClase.Stat.PENDING)
            return BadRequest("Solo se pueden modificar solicitudes en estado PENDING.");

        if (!Enum.TryParse<SolicitudClase.Stat>(
            dto.Status, true, out var nuevoEstado) ||
            (nuevoEstado != SolicitudClase.Stat.ACCEPTED &&
             nuevoEstado != SolicitudClase.Stat.DECLINED))
        {
            return BadRequest(
                "Estado inválido. Valores permitidos: ACCEPTED, DECLINED");
        }

        if (nuevoEstado == SolicitudClase.Stat.ACCEPTED)
        {
            if (!dto.AcademyId.HasValue || dto.AcademyId.Value <= 0)
                return BadRequest(
                    "Debés seleccionar una academia para aceptar la solicitud.");

            var academia = await _context.Academias
                .FirstOrDefaultAsync(a =>
                    a.Id == dto.AcademyId.Value &&
                    a.IsActive);

            if (academia == null)
                return BadRequest("La academia no existe o está inactiva.");

            var perteneceAcademia = await _context.DocentesAcademias
                .AnyAsync(da =>
                    da.TeacherId == solicitud.DocenteId &&
                    da.AcademyId == dto.AcademyId.Value &&
                    da.IsActive);

            if (!perteneceAcademia)
                return BadRequest(
                    "No pertenecés a esa academia o tu vínculo está inactivo.");

            solicitud.AcademyId = academia.Id;
            solicitud.Academy = academia;

            var ofertaMateria = await _context.DocentesMaterias
                .FirstOrDefaultAsync(dm =>
                    dm.TeacherId == solicitud.DocenteId &&
                    dm.MateriaId == solicitud.MateriaId &&
                    dm.Level == solicitud.Level);

            if (ofertaMateria == null)
                return BadRequest(
                    "No se encontró la oferta de materia y nivel del docente.");

            var clase = new ClaseAgendada
            {
                ScheduledAt = solicitud.Schedule,
                Duration = solicitud.Duration,
                Status = ClaseAgendada.Stat.SCHEDULED,
                Price = (float)((academia.PricePerHour ?? ofertaMateria.HourlyRate) * solicitud.Duration),
                Location = solicitud.Modalidad == ModalidadClase.ONLINE
                    ? "Online"
                    : solicitud.Docente.Location,
                RequestId = solicitud.Id,
                ParentId = solicitud.ParentId,
                TeacherId = solicitud.DocenteId,
                SubjectId = solicitud.MateriaId,
                AcademyId = academia.Id,
                ContentId = null
            };

            _context.ClasesAgendadas.Add(clase);
        }
        else
        {
            solicitud.AcademyId = null;
            solicitud.Academy = null;
        }

        solicitud.Status = nuevoEstado;

        await _context.SaveChangesAsync();

        var result = await ObtenerSolicitudDTO(solicitud.Id);

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

        if (s == null)
            return null;

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
            MateriaName = s.Materia?.Name ?? "",
            Level = s.Level
        };
    }
}
