namespace UDX.Server.DTOs
{
    public class ContenidoDTO
    {
        public string Title { get; set; }
        public IFormFile Archivo { get; set; }
        public int MateriaId { get; set; }
        public int HijoId { get; set; }
    }
}