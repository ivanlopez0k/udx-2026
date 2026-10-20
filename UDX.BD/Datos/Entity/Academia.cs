using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Academia : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }

        // Academia "personal" (oculta) que tiene cada docente para dar clases solo.
        public bool IsPersonal { get; set; }
        public bool IsActive { get; set; } = true;

        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Precios generales de la academia.
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerHour { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerCourse { get; set; }

        // Usuario responsable: es quien gestiona la academia.
        public int? ResponsableId { get; set; }
        public Usuario? Responsable { get; set; }

        public ICollection<DocenteAcademia> Members { get; set; }
    }
}
