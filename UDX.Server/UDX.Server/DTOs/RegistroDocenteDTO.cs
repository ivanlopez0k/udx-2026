
using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class RegistroDocenteDTO
    {
        [Required(ErrorMessage = "Ingresá tu nombre completo.")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(254)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Ingresá una contraseña.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [StringLength(100)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Ingresá tu zona.")]
        [StringLength(100)]
        public string Zona { get; set; } = "";

        [Required(ErrorMessage = "Seleccioná al menos una materia.")]
        [MinLength(1, ErrorMessage = "Seleccioná al menos una materia.")]
        public List<MateriaRegistroDTO> Materias { get; set; } = new();
    }

    public class MateriaRegistroDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "La materia seleccionada no es válida.")]
        public int MateriaId { get; set; }

        [Required(ErrorMessage = "Ingresá el nivel de la materia.")]
        public string Nivel { get; set; } = "";
        [Range(typeof(decimal), "0,01", "999999999",
            ErrorMessage = "El precio debe ser mayor que cero.")]
        public decimal PrecioPorHora { get; set; }
    }
}
