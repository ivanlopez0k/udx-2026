using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class EditarPrecioDTO
    {
        [Required(ErrorMessage = "El precio por hora es obligatorio.")]
        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal HourlyRate { get; set; }
    }
}
