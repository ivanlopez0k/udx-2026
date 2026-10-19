using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class CargarUbicacionDTO
    {
        [Required(ErrorMessage = "La ubicación es obligatoria.")]
        [MaxLength(200, ErrorMessage = "Máximo 200 caracteres.")]
        public string Location { get; set; }
    }
}
