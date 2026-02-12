namespace SafeGuard.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty; // EKLENDİ
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string PhoneNumber { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string ChronicDiseases { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public bool Smoker { get; set; }
        public bool AlcoholConsumption { get; set; }
        public bool IsSosActive { get; set; } = false;
        public string? HelperName { get; set; } = null; 
    }
}