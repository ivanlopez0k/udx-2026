using System.ComponentModel.DataAnnotations;

namespace UDX.Server.DTOs
{
    public class ContenidoDTO
    {
        public string Title { get; set; }
        public IFormFile Archivo { get; set; }
        public int MateriaId { get; set; }
        public int HijoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La academia es obligatoria.")]
        public int AcademyId { get; set; }
    }
}
