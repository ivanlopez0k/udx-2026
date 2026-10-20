using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Usuario : EntityBase
    {
        [Required]
        public string Email { get; set; }
        public string Password { get; set; }
        // Se guarda como int: agregar valores nuevos siempre al final.
        public enum Rol { PARENT, TEACHER, ADMIN, ACADEMY }
        public Rol Role { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? FotoPerfil { get; set; }
    }
}

