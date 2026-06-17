using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UDX.BD.Datos;

namespace UDX.Server.Authorization
{
    public class PerfilDocenteHandler : AuthorizationHandler<PerfilDocenteRequirement>
    {
        private readonly AppDbContext _context;

        public PerfilDocenteHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PerfilDocenteRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return;
            }

            bool tienePerfilDocente = await _context.PerfilesDocentes
                .AnyAsync(p => p.UserId == userId);

            if (tienePerfilDocente)
            {
                context.Succeed(requirement);
            }
        }
    }
}
