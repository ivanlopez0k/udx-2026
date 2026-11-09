namespace UDX.Shared.DTOs;

public class DocenteDetalleDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Credentials { get; set; } = "";
    public string Location { get; set; } = "";
    public float RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public bool Verified { get; set; }
    public string Availability { get; set; } = "";
    public List<AcademiaResumenDTO> Academias { get; set; } = new();
    public string Foto { get; set; } = "";
    public List<MateriaDetalleDTO> Materias { get; set; } = new();
}

public class AcademiaResumenDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class MateriaDetalleDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Grade { get; set; } = "";
    public string Category { get; set; } = "";
    public string Level { get; set; } = "";
    public decimal HourlyRate { get; set; }
}