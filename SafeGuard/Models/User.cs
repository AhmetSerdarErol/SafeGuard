namespace SafeGuard.Models
{
    public class User
    {
        public int Id { get; set; } // Birincil Anahtar (Primary Key)
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Admin veya User olabilir
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}