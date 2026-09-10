namespace UDX.Server.DTOs
{
    public class DocenteDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Location { get; set; }
        public decimal HourlyRate { get; set; }
        public float RatingAvg { get; set; }
        public int RatingCount { get; set; }
        public bool Verified { get; set; }
        public string Availability { get; set; }
        public List<string> Materias { get; set; }
    }

    public class BusquedaDocenteResponseDTO
    {
        public List<DocenteDTO> Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
