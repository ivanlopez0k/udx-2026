namespace UDX.Server.DTOs
{
    public class ClaseAgendadaDTO
    {
        public int Id { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
        public float Price { get; set; }
        public string Location { get; set; }

        public int RequestId { get; set; }

        public int ParentId { get; set; }
        public string ParentName { get; set; }

        public int TeacherId { get; set; }
        public string TeacherName { get; set; }

        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        public int AcademyId { get; set; }
        public string AcademyName { get; set; }
    }

    public class ActualizarEstadoClaseDTO
    {
        public string Status { get; set; }
    }
}
