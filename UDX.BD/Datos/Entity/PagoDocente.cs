using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class PagoDocente : EntityBase
    {
        public enum Stat
        {
            PENDING,
            PAID,
            FAILED
        }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public string Currency {  get; set; }
        public Stat Status { get; set; }
        public string ProcessorPayout { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TeacherId { get; set; }
        public PerfilDocente Teacher { get; set; }

        public int ClaseAgendadaId { get; set; }
        public ClaseAgendada claseAgendada { get; set; }
    }
}
