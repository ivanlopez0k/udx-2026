
namespace UDX.Shared.DTOs;

public class ProfesorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public bool Verified { get; set; }
    public string Availability { get; set; } = string.Empty;
    public List<string> Materias { get; set; } = new();
    public string Foto { get; set; } = string.Empty;
}
