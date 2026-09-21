using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Academia : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TeacherId { get; set; }
        public Usuario Teacher { get; set; }
    }
}
