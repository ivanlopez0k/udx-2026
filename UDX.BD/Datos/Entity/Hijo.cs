using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Hijo : EntityBase
    {
        public string Name { get; set; }
        public string Grade { get; set; }

        public int ParentId { get; set; }
        public Usuario Parent { get; set; }

    }
}
