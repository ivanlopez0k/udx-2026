using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Academia : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPersonal { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Usuario que creó la academia. No otorga permisos (ver DocenteAcademia.Role).
        public int? TeacherId { get; set; }
        public Usuario? Teacher { get; set; }

        public ICollection<DocenteAcademia> Members { get; set; }
    }
}
