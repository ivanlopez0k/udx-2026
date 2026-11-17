using System;

namespace UDX.Shared.DTOs;

public class ResenaDetalleDTO
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string AutorNombre { get; set; } = string.Empty;
    public int Calificacion { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
