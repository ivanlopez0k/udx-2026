using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.Shared.DTOs
{
    public class SolicitudClaseDTO
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
        public DateTime RequestedAt { get; set; }
        public DateTime Schedule { get; set; }
        public int Duration { get; set; }
        public string? Notes { get; set; }
        public string Message { get; set; } = "";
        public string Modalidad { get; set; } = "";

        public int ParentId { get; set; }
        public string ParentName { get; set; } = "";

        public int DocenteId { get; set; }
        public string DocenteName { get; set; } = "";

        public int? AcademyId { get; set; }
        public string AcademyName { get; set; } = "";

        public int MateriaId { get; set; }
        public string MateriaName { get; set; } = "";
        public string Level { get; set; } = "";
    }
}