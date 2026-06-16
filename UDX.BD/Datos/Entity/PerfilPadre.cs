using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class PerfilPadre : EntityBase
    {
        public string Location { get; set; }
        public bool Verified { get; set; }

        public int UserId { get; set; }
        public Usuario User { get; set; }

        public int HijoId { get; set; }
        public Hijo Hijo { get; set; }

    }
}
