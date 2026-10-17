using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class DocenteAcademia : EntityBase
    {
        public enum AcademyRole { DOCENTE, VICEDIRECTOR, DIRECTOR }
        public AcademyRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher { get; set; }

        public int AcademyId { get; set; }
        public Academia Academy { get; set; }
    }
}
