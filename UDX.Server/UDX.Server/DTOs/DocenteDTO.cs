namespace UDX.Server.DTOs
{
    public class DocenteDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Location { get; set; }
        public List<string> Materias { get; set; }
    }
}