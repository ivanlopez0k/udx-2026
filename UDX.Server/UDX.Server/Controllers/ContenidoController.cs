using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContenidoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ContenidoController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Los archivos se guardan fuera de wwwroot: solo se descargan por GET {id}/archivo.
        private string CarpetaContenidos =>
            Path.Combine(_env.ContentRootPath, "Storage", "contenidos");

        [HttpPost]
        [Authorize(Roles = "TEACHER")]
        public async Task<IActionResult> SubirMaterial([FromForm] ContenidoDTO dto)
        {
            var usuarioId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.UserId == usuarioId);

            if (perfil == null)
            {
                return NotFound("No se encontró el perfil docente.");
            }

            var esDocenteDeLaMateria = await _context.DocentesMaterias
                .AnyAsync(dm =>
                    dm.TeacherId == perfil.Id &&
                    dm.MateriaId == dto.MateriaId);

            if (!esDocenteDeLaMateria)
            {
                return BadRequest("El docente no está asociado a esa materia.");
            }

            var esMiembroActivo = await _context.DocentesAcademias
                .AnyAsync(da =>
                    da.TeacherId == perfil.Id &&
                    da.AcademyId == dto.AcademyId &&
                    da.IsActive);

            if (!esMiembroActivo)
            {
                return BadRequest("No sos miembro activo de esa academia.");
            }

            var hijo = await _context.Hijos
                .FirstOrDefaultAsync(h => h.Id == dto.HijoId);

            if (hijo == null)
            {
                return NotFound("No se encontró el alumno.");
            }

            var estaInscripto = await _context.SolicitudesClases
                .AnyAsync(s =>
                    s.Status == SolicitudClase.Stat.ACCEPTED &&
                    s.DocenteId == perfil.Id &&
                    s.AcademyId == dto.AcademyId &&
                    s.Parent.UserId == hijo.ParentId);

            if (!estaInscripto)
            {
                return BadRequest("El alumno no está inscripto con vos en esa academia.");
            }

            if (dto.Archivo == null || dto.Archivo.Length == 0)
            {
                return BadRequest("No se recibió ningún archivo.");
            }

            var carpeta = CarpetaContenidos;

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            var extension = Path.GetExtension(dto.Archivo.FileName);
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var ruta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await dto.Archivo.CopyToAsync(stream);
            }

            var contenido = new Contenido
            {
                Title = dto.Title,
                Text = "",
                CreatedAt = DateTime.UtcNow,
                TeacherId = usuarioId,
                ParentId = hijo.ParentId,
                FilePath = nombreArchivo,
                MateriaId = dto.MateriaId,
                HijoId = dto.HijoId,
                AcademyId = dto.AcademyId
            };
            await _context.Contenidos.AddAsync(contenido);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<List<ContenidoResponseDTO>>> Listar(
            [FromQuery] int? materiaId,
            [FromQuery] int? hijoId,
            [FromQuery] int? academyId)
        {
            var query = FiltrarAccesibles(_context.Contenidos, UsuarioId(), Rol());

            if (materiaId.HasValue)
            {
                query = query.Where(c => c.MateriaId == materiaId.Value);
            }

            if (hijoId.HasValue)
            {
                query = query.Where(c => c.HijoId == hijoId.Value);
            }

            if (academyId.HasValue)
            {
                query = query.Where(c => c.AcademyId == academyId.Value);
            }

            var lista = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ContenidoResponseDTO
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    MateriaId = c.MateriaId,
                    MateriaName = c.Materia.Name,
                    HijoId = c.HijoId,
                    HijoName = c.Hijo.Name,
                    AcademyId = c.AcademyId,
                    AcademyName = c.Academy != null ? c.Academy.Name : null,
                    TeacherName = c.Teacher.Name,
                    DownloadUrl = "/api/Contenido/" + c.Id + "/archivo"
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<ContenidoResponseDTO>> Obtener(int id)
        {
            var contenido = await _context.Contenidos
                .Include(c => c.Materia)
                .Include(c => c.Hijo)
                .Include(c => c.Academy)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contenido == null)
            {
                return NotFound("No se encontró el material.");
            }

            if (!await PuedeAcceder(contenido, UsuarioId(), Rol()))
            {
                return Forbid();
            }

            return Ok(new ContenidoResponseDTO
            {
                Id = contenido.Id,
                Title = contenido.Title,
                CreatedAt = contenido.CreatedAt,
                MateriaId = contenido.MateriaId,
                MateriaName = contenido.Materia.Name,
                HijoId = contenido.HijoId,
                HijoName = contenido.Hijo.Name,
                AcademyId = contenido.AcademyId,
                AcademyName = contenido.Academy?.Name,
                TeacherName = contenido.Teacher.Name,
                DownloadUrl = $"/api/Contenido/{contenido.Id}/archivo"
            });
        }

        [HttpGet("{id}/archivo")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Descargar(int id)
        {
            var contenido = await _context.Contenidos
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contenido == null)
            {
                return NotFound("No se encontró el material.");
            }

            if (!await PuedeAcceder(contenido, UsuarioId(), Rol()))
            {
                return Forbid();
            }

            // GetFileName evita que un FilePath raro se salga de la carpeta.
            var nombreArchivo = Path.GetFileName(contenido.FilePath ?? "");
            var ruta = Path.Combine(CarpetaContenidos, nombreArchivo);

            if (string.IsNullOrEmpty(nombreArchivo) || !System.IO.File.Exists(ruta))
            {
                return NotFound("No se encontró el archivo.");
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(nombreArchivo, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var nombreDescarga = contenido.Title + Path.GetExtension(nombreArchivo);
            return PhysicalFile(ruta, contentType, nombreDescarga);
        }

        // Regla de acceso: ADMIN, el docente que lo subió, el responsable de la academia,
        // o el padre del alumno con una inscripción ACCEPTED con ese docente en esa academia.
        private async Task<bool> PuedeAcceder(Contenido contenido, int userId, string? rol)
        {
            return await FiltrarAccesibles(_context.Contenidos, userId, rol)
                .AnyAsync(c => c.Id == contenido.Id);
        }

        // Misma regla que PuedeAcceder, como filtro de query para el listado.
        private IQueryable<Contenido> FiltrarAccesibles(IQueryable<Contenido> query, int userId, string? rol)
        {
            if (rol == "ADMIN")
            {
                return query;
            }

            return query.Where(c =>
                c.TeacherId == userId ||
                (c.Academy != null && c.Academy.ResponsableId == userId) ||
                (c.ParentId == userId &&
                 c.AcademyId != null &&
                 _context.SolicitudesClases.Any(s =>
                     s.Status == SolicitudClase.Stat.ACCEPTED &&
                     s.AcademyId == c.AcademyId &&
                     s.Parent.UserId == userId &&
                     s.Docente.UserId == c.TeacherId)));
        }

        private int UsuarioId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string? Rol() =>
            User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
