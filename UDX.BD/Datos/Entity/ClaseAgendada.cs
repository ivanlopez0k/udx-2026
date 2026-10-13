using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class ClaseAgendada : EntityBase
    {
        public enum Stat
        {
            SCHEDULED, 
            COMPLETED,
            CANCELED,
            NO_SHOW,
            REFUNDED
        }

        public DateTime ScheduledAt { get; set; }
        public int Duration { get; set; }
        public Stat Status { get; set; }
        public float Price { get; set; }
        public string Location { get; set; }
        
        public int RequestId { get; set; }
        public SolicitudClase Request { get; set; }

        public int ParentId { get; set; }
        public PerfilPadre Parent { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher {  get; set; }

        public int SubjectId { get; set; }
        public Materia Subject { get; set; }

        public int AcademyId { get; set; }
        public Academia Academy { get; set; }

        public int? ContentId { get; set; }
        public Contenido? Content { get; set; }

    }
}
