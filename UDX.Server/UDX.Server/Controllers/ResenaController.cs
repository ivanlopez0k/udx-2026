using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UDX.BD.Datos;
using UDX.BD.Datos.Entity;
using UDX.Shared.DTO;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResenaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResenaController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public IActionResult Post(ResenaDTO dto)
        {
            int autorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            Resena entidad = new Resena
            {
                TeacherId = dto.TeacherId,
                authorResena = autorId,
                Rating = dto.Calificacion,
                Comment = dto.Comentario,
                CreatedAt = DateTime.UtcNow
            };

            _context.Resenas.Add(entidad);
            _context.SaveChanges();

            return Ok(entidad.Id);
        }
    }
}