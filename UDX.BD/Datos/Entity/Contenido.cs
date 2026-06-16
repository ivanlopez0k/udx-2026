using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Contenido : EntityBase
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TeacherId { get; set; }
        public Usuario Teacher { get; set; }

        public int ParentId { get; set; }
        public Usuario Parent { get; set; }
    }
}
