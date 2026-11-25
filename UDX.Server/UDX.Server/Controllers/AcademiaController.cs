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
            IsActive = academia.IsActive,
            PricePerHour = academia.PricePerHour,
            PricePerCourse = academia.PricePerCourse
        };

        return CreatedAtAction(nameof(GetById), new { id = academia.Id }, result);
    }

    // GET: api/Academia (lista academias activas, no personales)
    [HttpGet]
    public async Task<ActionResult<List<AcademiaDTO>>> GetAll()
    {
        var academias = await _context.Academias
            .Where(a => !a.IsPersonal && a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => new AcademiaDTO
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Code = a.Code,
                CreatedAt = a.CreatedAt,
                Location = a.Location,
                Phone = a.Phone,
                Email = a.Email,
                ResponsableId = a.ResponsableId,
                IsActive = a.IsActive,
                PricePerHour = a.PricePerHour,
                PricePerCourse = a.PricePerCourse
            })
            .ToListAsync();

        return Ok(academias);
    }

    // GET: api/Academia/mia (academia oficial a cargo del usuario autenticado)
    [Authorize]
    [HttpGet("mia")]
    public async Task<ActionResult<AcademiaDTO>> GetMiAcademia()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var academia = await _context.Academias
            .FirstOrDefaultAsync(a => a.ResponsableId == userId && !a.IsPersonal);

        if (academia == null)
            return NotFound("No tenés una academia a tu cargo.");

        return Ok(MapearDTO(academia));
    }

    // GET: api/Academia/publico/{nombre} (vista pública, sin autenticación - busca por nombre)
    [HttpGet("publico/nombre/{nombre}")]
    public async Task<ActionResult<AcademiaDTO>> GetPublicoPorNombre(string nombre)
    {
        var academia = await _context.Academias
            .Where(a => a.Name.ToLower() == nombre.ToLower() && !a.IsPersonal && a.IsActive)
            .Select(a => new AcademiaDTO
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Code = a.Code,
                CreatedAt = a.CreatedAt,
                Location = a.Location,
                Phone = a.Phone,
                Email = a.Email,
                ResponsableId = a.ResponsableId,
                IsActive = a.IsActive,
                PricePerHour = a.PricePerHour,
                PricePerCourse = a.PricePerCourse
            })
            .FirstOrDefaultAsync();

        if (academia == null)
            return NotFound("Academia no encontrada.");

        return Ok(academia);
    }

    // GET: api/Academia/publico/{codigo} (vista pública, sin autenticación - busca por código)
    [HttpGet("publico/{codigo}")]
    public async Task<ActionResult<AcademiaDTO>> GetPublicoPorCodigo(string codigo)
    {
        var academia = await _context.Academias
            .Where(a => a.Code.ToUpper() == codigo.ToUpper() && !a.IsPersonal && a.IsActive)
            .Select(a => new AcademiaDTO
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Code = a.Code,
                CreatedAt = a.CreatedAt,
                Location = a.Location,
                Phone = a.Phone,
                Email = a.Email,
                ResponsableId = a.ResponsableId,
                IsActive = a.IsActive,
                PricePerHour = a.PricePerHour,
                PricePerCourse = a.PricePerCourse
            })
            .FirstOrDefaultAsync();

        if (academia == null)
            return NotFound("Academia no encontrada.");

        return Ok(academia);
    }

    // GET: api/Academia/{id}/docentes (vista pública, sin autenticación)
    [HttpGet("{id:int}/docentes")]
    public async Task<ActionResult<List<DocenteAcademiaDTO>>> GetDocentesPorAcademia(int id)
    {
        var membresias = await _context.DocentesAcademias
            .Where(da => da.AcademyId == id && da.IsActive)
            .Include(da => da.Teacher)
                .ThenInclude(p => p.User)
            .Include(da => da.Teacher)
                .ThenInclude(p => p.DocentesMaterias)
                    .ThenInclude(dm => dm.Materia)
            .ToListAsync();

        var docentes = membresias
            .OrderBy(da => da.Teacher.User?.Name ?? string.Empty)
            .Select(da => new DocenteAcademiaDTO
            {
                Id = da.Teacher.Id,
                Nombre = da.Teacher.User?.Name ?? "Docente",
                Foto = da.Teacher.ProfileImage,
                Location = da.Teacher.Location,
                Credenciales = da.Teacher.Credentials,
                Calificacion = da.Teacher.RatingAvg,
                CantidadResenas = da.Teacher.RatinCount,
                Verificado = da.Teacher.Verified,
                Disponibilidad = da.Teacher.Availability,
                Materias = da.Teacher.DocentesMaterias?
                    .Where(dm => dm.Materia != null)
                    .Select(dm => dm.Materia.Name)
                    .ToList() ?? new List<string>()
            })
            .ToList();

        return Ok(docentes);
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
            IsActive = academia.IsActive,
            PricePerHour = academia.PricePerHour,
            PricePerCourse = academia.PricePerCourse
        });
    }

    // PUT: api/Academia/{id}/precios (solo el responsable, incluye academias personales)
    [Authorize]
    [HttpPut("{id:int}/precios")]
    public async Task<IActionResult> ActualizarPrecios(int id, [FromBody] ActualizarPreciosAcademiaDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var academia = await _context.Academias
            .FirstOrDefaultAsync(a => a.Id == id);

        if (academia == null)
            return NotFound("Academia no encontrada.");

        if (academia.ResponsableId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "Solo el responsable puede modificar los precios.");

        // null borra el precio
        academia.PricePerHour = dto.PricePerHour;
        academia.PricePerCourse = dto.PricePerCourse;

        await _context.SaveChangesAsync();

        return Ok(MapearDTO(academia));
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

    // PUT: api/Academia/{id} (solo el responsable) - descripción, ubicación, teléfono y mail
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(
        int id,
        [FromBody] EditarAcademiaDTO dto)
    {
        var (academia, error) = await ObtenerDelResponsable(id);
        if (error != null) return error;

        academia!.Description = dto.Description.Trim();
        academia.Location = dto.Location?.Trim();
        academia.Phone = dto.Phone?.Trim();
        academia.Email = dto.Email?.Trim();

        await _context.SaveChangesAsync();

        return Ok(MapearDTO(academia));
    }

    // PUT: api/Academia/{id}/estado (solo el responsable) - activar o desactivar a mano
    [Authorize]
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoAcademiaDTO dto)
    {
        var (academia, error) = await ObtenerDelResponsable(id);
        if (error != null) return error;

        if (dto.IsActive == null)
            return BadRequest("El campo isActive es obligatorio.");

        academia!.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(MapearDTO(academia));
    }

    // DELETE: api/Academia/{id}/cuenta (solo el responsable)
    [Authorize]
    [HttpDelete("{id:int}/cuenta")]
    public async Task<IActionResult> EliminarCuenta(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var academia = await _context.Academias
            .FirstOrDefaultAsync(a => a.Id == id);

        if (academia == null)
            return NotFound("Academia no encontrada.");

        if (academia.ResponsableId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, "Solo el responsable puede eliminar la academia.");

        _context.Academias.Remove(academia);
        await _context.SaveChangesAsync();

        return Ok("Academia eliminada correctamente.");
    }

    // PUT: api/Academia/{id}/responsable (solo el responsable actual)
    [Authorize]
    [HttpPut("{id:int}/responsable")]
    public async Task<IActionResult> CambiarResponsable(
        int id,
        [FromBody] CambiarResponsableAcademiaDTO dto)
    {
        var (academia, error) = await ObtenerDelResponsable(id);
        if (error != null) return error;

        var nuevo = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == dto.ResponsableId);

        if (nuevo == null)
            return BadRequest("El usuario indicado no existe.");

        if (nuevo.Role != Usuario.Rol.TEACHER && nuevo.Role != Usuario.Rol.ACADEMY)
            return BadRequest("El responsable debe ser un docente o una cuenta de academia.");

        academia!.ResponsableId = nuevo.Id;

        await _context.SaveChangesAsync();

        return Ok(MapearDTO(academia));
    }

    // Devuelve la academia si el usuario autenticado es su responsable; si no, el error a devolver.
    private async Task<(Academia? academia, IActionResult? error)> ObtenerDelResponsable(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var academia = await _context.Academias
            .FirstOrDefaultAsync(a => a.Id == id);

        if (academia == null)
            return (null, NotFound("Academia no encontrada."));

        if (academia.ResponsableId != userId)
            return (null, StatusCode(StatusCodes.Status403Forbidden, "Solo el responsable puede editar la academia."));

        if (academia.IsPersonal)
            return (null, BadRequest("No se puede editar una academia personal."));

        return (academia, null);
    }

    private static AcademiaDTO MapearDTO(Academia academia) => new()
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
        IsActive = academia.IsActive,
        PricePerHour = academia.PricePerHour,
        PricePerCourse = academia.PricePerCourse
    };

    public class DocenteAcademiaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Foto { get; set; }
        public string? Location { get; set; }
        public string? Credenciales { get; set; }
        public float Calificacion { get; set; }
        public int CantidadResenas { get; set; }
        public bool Verificado { get; set; }
        public string? Disponibilidad { get; set; }
        public List<string> Materias { get; set; } = new();
    }
}
