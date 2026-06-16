using System;
using System.Collections.Generic;
using System.Text;

namespace UDX.BD.Datos.Entity
{
    public class Mensaje : EntityBase
    {
        public string Body { get; set; }
        public DateTime SentAt { get; set; }
        public bool isRead { get; set; }

        public int senderId { get; set; }
        public Usuario Sender { get; set; }

        public int ReceiverId { get; set; }
        public Usuario Receiver { get; set; }
    }
}
