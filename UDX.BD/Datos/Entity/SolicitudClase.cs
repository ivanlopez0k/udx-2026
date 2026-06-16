using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class SolicitudClase : EntityBase
    {
        public enum Stat
        {
            PENDING,
            ACCEPTED,
            DECLINED,
            EXPIRED
        }

        public Stat Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime Schedule { get; set;  }
        public DateTime Duration { get; set; }
        public int Notes { get; set; }
        public string Message { get; set; }

        public int parentId {  get; set; }
        public PerfilPadre Parent {  get; set; }

        public int AcademyId { get; set; }
        public Academia Academy { get; set; }
        
        public int DocenteId { get; set; }
        public PerfilDocente Docente { get; set; }

        public int MateriaId { get; set; }
        public Materia Materia { get; set; }

    }
}
