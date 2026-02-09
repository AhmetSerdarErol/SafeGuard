namespace SafeGuard.Models
{
    public class User
    {
        public Guid Id { get; set; } // Benzersiz kimlik
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? BloodType { get; set; } // Boş bırakılabilir
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}