using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class ActualizarPreciosAcademiaDTO
    {
        // null = sin precio (se borra el valor)
        [Range(0.0, 99999999.0, ErrorMessage = "El precio por hora debe estar entre 0 y 99999999")]
        public decimal? PricePerHour { get; set; }

        [Range(0.0, 99999999.0, ErrorMessage = "El precio por curso debe estar entre 0 y 99999999")]
        public decimal? PricePerCourse { get; set; }
    }
}
