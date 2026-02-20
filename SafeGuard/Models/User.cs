namespace SafeGuard.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty; 
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string PhoneNumber { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;       
        public string ChronicDiseases { get; set; } = string.Empty; 
        public string Allergies { get; set; } = string.Empty;       
        public string Surgeries { get; set; } = string.Empty; 
        public string Habits { get; set; } = string.Empty;
        public bool Smoker { get; set; }
        public bool AlcoholConsumption { get; set; }
        public bool IsSosActive { get; set; } = false;
        public string? HelperName { get; set; } = null;
        public string? ProfilePhotoUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Height { get; set; }      
        public int? Weight { get; set; }      
        public string? PrimaryLanguage { get; set; }
        public string? OrganStatus { get; set; } 
        public string? OrganDetails { get; set; } 
        public string? MedicalConditions { get; set; } 
        public string? Medications { get; set; }       
        public string? MedicalNotes { get; set; }      
        public string? AlcoholUse { get; set; }  
        public string? SmokingHabit { get; set; }
        public string Password { get; set; }
        public string? BirthDate { get; set; }
    }
}