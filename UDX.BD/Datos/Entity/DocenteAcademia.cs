using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class DocenteAcademia : EntityBase
    {
        public DateTime JoinedAt { get; set; }

        // Salir o ser removido = IsActive false + LeftAt (no se borra la fila).
        public bool IsActive { get; set; } = true;
        public DateTime? LeftAt { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher { get; set; }

        public int AcademyId { get; set; }
        public Academia Academy { get; set; }
    }
}
