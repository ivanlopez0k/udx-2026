using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Server.Authorization;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MateriaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MateriaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = "PerfilDocenteOnly")]
        public async Task<IActionResult> CrearMateria(CrearMateriaDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim!.Value);

            var perfilDocente = await _context.PerfilesDocentes
                .FirstAsync(p => p.UserId == userId);

            var materia = new Materia
            {
                Name = dto.Name,
                Grade = dto.Grade,
                Category = dto.Category
            };

            _context.Materias.Add(materia);
            await _context.SaveChangesAsync();

            var docenteMateria = new DocenteMateria
            {
                TeacherId = perfilDocente.Id,
                MateriaId = materia.Id,
                Level = "General"
            };

            _context.DocentesMaterias.Add(docenteMateria);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Materia = materia,
                DocenteMateria = docenteMateria
            });
        }

        [HttpGet]
        [Authorize(Policy = "PerfilDocenteOnly")]
        public async Task<IActionResult> ListarMisMaterias()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim!.Value);

            var perfilDocente = await _context.PerfilesDocentes
                .FirstAsync(p => p.UserId == userId);

            var materias = await _context.DocentesMaterias
                .Where(dm => dm.TeacherId == perfilDocente.Id)
                .Include(dm => dm.Materia)
                .Select(dm => new
                {
                    dm.Id,
                    dm.Level,
                    Materia = dm.Materia
                })
                .ToListAsync();

            return Ok(materias);
        }
    }
}
