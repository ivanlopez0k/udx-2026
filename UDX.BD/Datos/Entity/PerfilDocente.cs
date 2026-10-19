using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace UDX.BD.Datos.Entity


{
    public class PerfilDocente : EntityBase
    {
        public string Credentials { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }
        public string Location { get; set; }
        public float RatingAvg { get; set; }
        public int RatinCount { get; set; }
        public string Availability { get; set; }
        public bool Verified { get; set; }
        public string? ProfileImage { get; set; }

        public int UserId { get; set; }
        public Usuario User { get; set; }

        public int AcademyId { get; set; }
        public Academia Academy { get; set; }

        public ICollection<DocenteMateria> DocentesMaterias { get; set; }

    }
}
