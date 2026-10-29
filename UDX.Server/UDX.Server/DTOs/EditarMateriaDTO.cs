using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class EditarMateriaDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Grade { get; set; }

        [Required]
        public string Category { get; set; }
    }
}
