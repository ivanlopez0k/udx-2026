namespace UDX.Shared.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = "";
        public string Email { get; set; } = "";
        public int Role { get; set; }
    }
}