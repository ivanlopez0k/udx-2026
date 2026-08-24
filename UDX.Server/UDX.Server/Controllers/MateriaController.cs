using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ObtenerMaterias()
        {
            var materias = await _context.Materias
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Grade,
                    m.Category
                })
                .ToListAsync();

            return Ok(materias);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CrearMateria(CrearMateriaDTO dto)
        {
            var existe = await _context.Materias.AnyAsync(m =>
                m.Name.ToLower() == dto.Name.ToLower() &&
                m.Grade.ToLower() == dto.Grade.ToLower());

            if (existe)
            {
                return Conflict(new { Message = "Ya existe una materia con ese nombre y grado." });
            }

            var materia = new Materia
            {
                Name = dto.Name,
                Grade = dto.Grade,
                Category = dto.Category
            };

            _context.Materias.Add(materia);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                materia.Id,
                materia.Name,
                materia.Grade,
                materia.Category
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> EditarMateria(int id, EditarMateriaDTO dto)
        {
            var materia = await _context.Materias.FindAsync(id);
            if (materia == null)
            {
                return NotFound();
            }

            var duplicada = await _context.Materias.AnyAsync(m =>
                m.Id != id &&
                m.Name.ToLower() == dto.Name.ToLower() &&
                m.Grade.ToLower() == dto.Grade.ToLower());

            if (duplicada)
            {
                return Conflict(new { Message = "Ya existe una materia con ese nombre y grado." });
            }

            materia.Name = dto.Name;
            materia.Grade = dto.Grade;
            materia.Category = dto.Category;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
