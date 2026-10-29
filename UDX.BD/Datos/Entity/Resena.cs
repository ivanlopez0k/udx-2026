using System;
using System.Collections.Generic;
using System.Text;


namespace UDX.BD.Datos.Entity
{
    public class Resena : EntityBase
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher {  get; set; }

        public int authorResena { get; set; }
        public Usuario Author { get; set; }
    }
}
