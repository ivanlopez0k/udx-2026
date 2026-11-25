namespace UDX.Server.Client.Models
{
    public class AcademiaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Code { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? ResponsableId { get; set; }
        public bool IsActive { get; set; }
        public decimal? PricePerHour { get; set; }
        public decimal? PricePerCourse { get; set; }
    }
}