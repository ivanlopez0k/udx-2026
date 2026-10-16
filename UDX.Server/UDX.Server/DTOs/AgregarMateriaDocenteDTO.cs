namespace UDX.Server.DTOs
{
    public class AgregarMateriaDocenteDTO
    {
        public int MateriaId { get; set; }

        public string Nivel { get; set; } = "";

        public decimal PrecioPorHora { get; set; }
    }
}