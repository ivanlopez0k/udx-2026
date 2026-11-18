namespace UDX.Server.DTOs
{
    public class ActualizarPerfilDocenteDTO
    {
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Credenciales { get; set; } = "";
        public decimal PrecioPorHora { get; set; }
        public string Ubicacion { get; set; } = "";
        public string Disponibilidad { get; set; } = "";
    }
}