using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class DocenteMateria : EntityBase
    {
        public string Level { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher { get; set; }

        public int MateriaId { get; set; }
        public Materia Materia { get; set; }

    }
}
