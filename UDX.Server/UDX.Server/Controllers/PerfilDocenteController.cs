using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UDX.BD.Datos;
using UDX.Server.DTOs;

namespace UDX.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerfilDocenteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PerfilDocenteController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("ubicacion")]
        [Authorize(Policy = "PerfilDocenteOnly")]
        public async Task<IActionResult> CargarUbicacion(CargarUbicacionDTO dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (perfil == null)
            {
                return NotFound("No se encontró el perfil docente.");
            }

            if (!string.IsNullOrWhiteSpace(perfil.Location))
            {
                return BadRequest("Ya tenés una ubicación cargada. Usá el endpoint de editar ubicación para modificarla.");
            }

            perfil.Location = dto.Location;
            await _context.SaveChangesAsync();

            return Ok(new { perfil.Id, perfil.Location });
        }

[HttpPut("ubicacion")]
        [Authorize(Policy = "PerfilDocenteOnly")]
        public async Task<IActionResult> EditarUbicacion(CargarUbicacionDTO dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (perfil == null)
            {
                return NotFound("No se encontró el perfil docente.");
            }

            perfil.Location = dto.Location;
            await _context.SaveChangesAsync();

            return Ok(new { perfil.Id, perfil.Location });
        }
        
        [HttpPut("precio")]
        [Authorize(Policy = "PerfilDocenteOnly")]
        public async Task<IActionResult> EditarPrecio(EditarPrecioDTO dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var perfil = await _context.PerfilesDocentes
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (perfil == null)
            {
                return NotFound("No se encontró el perfil docente.");
            }

            perfil.HourlyRate = dto.HourlyRate;
            await _context.SaveChangesAsync();

            return Ok(new { perfil.Id, perfil.HourlyRate });
        }
    }
}
