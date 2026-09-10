using System.ComponentModel.DataAnnotations;

namespace UDX.Shared.DTO
{
    public class ResenaDTO
    {
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe ser entre 1 y 5.")]
        public int Calificacion { get; set; }

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres.")]
        public string Comentario { get; set; } = string.Empty;
    }
}