using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDX.BD.Datos.Entity
{
    public class DocenteMateria : EntityBase
    {
        public string Level { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher { get; set; }

        public int MateriaId { get; set; }
        public Materia Materia { get; set; }
    }
}