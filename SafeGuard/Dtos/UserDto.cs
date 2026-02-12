namespace SafeGuard.Dtos
{
    public class UserDto
    {
        public string FullName { get; set; } = string.Empty; // EKLENDİ
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string ChronicDiseases { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public bool Smoker { get; set; }
        public bool AlcoholConsumption { get; set; }
    }
}