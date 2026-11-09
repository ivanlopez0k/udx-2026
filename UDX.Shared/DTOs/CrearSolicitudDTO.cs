using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.Shared.DTOs
{
    public class CrearSolicitudDTO
    {
        public int ParentId { get; set; }
        public int DocenteId { get; set; }
        
        public int MateriaId { get; set; }
        public string Level { get; set; } = "";
        public DateTimeOffset Schedule { get; set; }
        public int Duration { get; set; }
        public string? Notes { get; set; }
        public string Message { get; set; } = "";
        public string Modalidad { get; set; } = "";
    }
}