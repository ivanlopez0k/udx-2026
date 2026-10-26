
namespace UDX.Shared.DTOs;

public class ProfesoresPaginadosDto
{
    public List<ProfesorDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
