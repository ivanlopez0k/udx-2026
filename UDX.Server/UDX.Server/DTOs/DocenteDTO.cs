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

    public class DocenteDetalleDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Credentials { get; set; }
        public decimal HourlyRate { get; set; }
        public string Location { get; set; }
        public float RatingAvg { get; set; }
        public int RatingCount { get; set; }
        public bool Verified { get; set; }
        public string Availability { get; set; }
        public string Academy { get; set; }
        public List<MateriaDetalleDTO> Materias { get; set; }
    }

    public class MateriaDetalleDTO
    {
        public string Name { get; set; }
        public string Grade { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
    }
}
