namespace UDX.Server.DTOs
{
    public class ContenidoResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MateriaId { get; set; }
        public string MateriaName { get; set; } = string.Empty;
        public int HijoId { get; set; }
        public string HijoName { get; set; } = string.Empty;
        public int? AcademyId { get; set; }
        public string? AcademyName { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
