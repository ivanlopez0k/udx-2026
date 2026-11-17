using System.ComponentModel.DataAnnotations;

namespace UDX.Shared.DTOs;

public class ResenaDTO
{
    public int Id { get; set; }

    [Required]
    public int TeacherId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "La calificación debe ser entre 1 y 5 estrellas.")]
    public int Calificacion { get; set; }

    [Required(ErrorMessage = "Por favor escribí un comentario sobre tu experiencia pedagógica.")]
    [MinLength(5, ErrorMessage = "El comentario debe tener al menos 5 caracteres.")]
    [MaxLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
    public string Comentario { get; set; } = string.Empty;
}
