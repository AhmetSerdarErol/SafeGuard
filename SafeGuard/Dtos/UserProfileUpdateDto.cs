namespace SafeGuard.Dtos 
{
    public class UserProfileUpdateDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string? BloodType { get; set; }
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? Medications { get; set; }
        public string? OrganStatus { get; set; }
        public string? OrganDetails { get; set; }
        public string? AlcoholUse { get; set; }
        public string? SmokingHabit { get; set; }
    }
}