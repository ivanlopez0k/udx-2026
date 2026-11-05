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
            return StatusCode(StatusCodes.Status403Forbidden, "No podés crear solicitudes para otro padre.");

        // Validate academy exists and is active
        var academia = await _context.Academias.FindAsync(dto.AcademyId);
        if (academia == null)
            return BadRequest("No se encontró la academia.");

        if (!academia.IsActive)
            return BadRequest("La academia no está activa.");

        // Validate materia exists
        var materia = await _context.Materias.FindAsync(dto.MateriaId);
        if (materia == null)
            return BadRequest("No se encontró la materia.");

        // Parse modalidad
        if (!Enum.TryParse<ModalidadClase>(dto.Modalidad, true, out var modalidad))
            return BadRequest("Modalidad inválida. Valores permitidos: ONLINE, PRESENCIAL");

        // dto.DocenteId se ignora: en una academia personal el docente es su único miembro activo;
        // en una oficial lo asigna el responsable al aceptar.
        int? docenteId = null;

        if (academia.IsPersonal)
        {
            var miembro = await _context.DocentesAcademias
                .FirstOrDefaultAsync(da => da.AcademyId == academia.Id && da.IsActive);

            if (miembro == null)
                return BadRequest("La academia no tiene un docente activo.");

            if (!await DictaMateria(miembro.TeacherId, dto.MateriaId))
                return BadRequest("El docente no dicta esa materia.");

            docenteId = miembro.TeacherId;
        }
        else
        {
            var hayDocentes = await _context.DocentesAcademias
                .AnyAsync(da => da.AcademyId == academia.Id && da.IsActive &&
                    _context.DocentesMaterias.Any(dm => dm.TeacherId == da.TeacherId && dm.MateriaId == dto.MateriaId));

            if (!hayDocentes)
                return BadRequest("La academia no tiene docentes para esa materia.");
        }

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
            DocenteId = docenteId,
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
            return StatusCode(StatusCodes.Status403Forbidden, "No podés ver solicitudes de otro docente.");

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

    // GET: api/SolicitudClase/academia/{academyId}?status=PENDING (solo el responsable de la academia)
    [Authorize]
    [HttpGet("academia/{academyId:int}")]
    public async Task<ActionResult<List<SolicitudClaseDTO>>> GetByAcademia(
        int academyId,
        [FromQuery] string? status = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var academia = await _context.Academias.FindAsync(academyId);

        if (academia == null)
            return NotFound("No se encontró la academia.");

        if (academia.ResponsableId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "Solo el responsable de la academia puede ver sus solicitudes.");

        var query = _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d!.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .Where(s => s.AcademyId == academyId)
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

        return Ok(solicitudes.Select(MapearDTO).ToList());
    }

    // PUT: api/SolicitudClase/{id}/estado (solo el responsable de la academia acepta/rechaza)
    [Authorize]
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<SolicitudClaseDTO>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoSolicitudDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var solicitud = await _context.SolicitudesClases
            .Include(s => s.Academy)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound("No se encontró la solicitud.");

        if (solicitud.Academy.ResponsableId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "Solo el responsable de la academia puede responder la solicitud.");

        if (solicitud.Status != SolicitudClase.Stat.PENDING)
            return BadRequest("Solo se pueden modificar solicitudes en estado PENDING.");

        SolicitudClase.Stat nuevoEstado;
        if (string.Equals(dto.Status, nameof(SolicitudClase.Stat.ACCEPTED), StringComparison.OrdinalIgnoreCase))
            nuevoEstado = SolicitudClase.Stat.ACCEPTED;
        else if (string.Equals(dto.Status, nameof(SolicitudClase.Stat.DECLINED), StringComparison.OrdinalIgnoreCase))
            nuevoEstado = SolicitudClase.Stat.DECLINED;
        else
            return BadRequest("Estado inválido. Valores permitidos: ACCEPTED, DECLINED");

        if (nuevoEstado == SolicitudClase.Stat.ACCEPTED)
        {
            // En una academia oficial el responsable asigna el docente; en una personal ya viene asignado.
            if (!solicitud.Academy.IsPersonal)
            {
                if (dto.DocenteId == null)
                    return BadRequest("Hay que indicar el docente para aceptar la solicitud.");

                var esMiembroActivo = await _context.DocentesAcademias
                    .AnyAsync(da => da.AcademyId == solicitud.AcademyId && da.TeacherId == dto.DocenteId && da.IsActive);

                if (!esMiembroActivo)
                    return BadRequest("El docente no es miembro activo de la academia.");

                if (!await DictaMateria(dto.DocenteId.Value, solicitud.MateriaId))
                    return BadRequest("El docente no dicta la materia de la solicitud.");

                solicitud.DocenteId = dto.DocenteId;
            }

            if (solicitud.DocenteId == null)
                return BadRequest("La solicitud no tiene un docente asignado.");

            var precioPorHora = solicitud.Academy.PricePerHour;

            var clase = new ClaseAgendada
            {
                ScheduledAt = solicitud.Schedule,
                Duration = solicitud.Duration,
                Status = ClaseAgendada.Stat.SCHEDULED,
                Price = precioPorHora.HasValue ? (float)(precioPorHora.Value * solicitud.Duration / 60) : 0,
                Location = "",
                RequestId = solicitud.Id,
                ParentId = solicitud.ParentId,
                TeacherId = solicitud.DocenteId.Value,
                SubjectId = solicitud.MateriaId,
                AcademyId = solicitud.AcademyId,
                ContentId = null
            };

            _context.ClasesAgendadas.Add(clase);
        }

        solicitud.Status = nuevoEstado;

        await _context.SaveChangesAsync();

        // Se recarga con los User incluidos para que parentName y docenteName no vuelvan vacíos
        return Ok(await ObtenerSolicitudDTO(solicitud.Id));
    }

    private Task<bool> DictaMateria(int docenteId, int materiaId) =>
        _context.DocentesMaterias.AnyAsync(dm => dm.TeacherId == docenteId && dm.MateriaId == materiaId);

    private async Task<SolicitudClaseDTO?> ObtenerSolicitudDTO(int id)
    {
        var s = await _context.SolicitudesClases
            .Include(s => s.Parent).ThenInclude(p => p.User)
            .Include(s => s.Docente).ThenInclude(d => d!.User)
            .Include(s => s.Academy)
            .Include(s => s.Materia)
            .FirstOrDefaultAsync(s => s.Id == id);

        return s == null ? null : MapearDTO(s);
    }

    private static SolicitudClaseDTO MapearDTO(SolicitudClase s)
    {
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
