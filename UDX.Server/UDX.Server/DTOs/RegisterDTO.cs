using System.ComponentModel.DataAnnotations;
using UDX.BD.Datos.Entity;

public class RegisterDTO
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string Name { get; set; }

    public string Phone { get; set; }

    public Usuario.Rol Role { get; set; }

    public string? AcademyCode { get; set; }
}