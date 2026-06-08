using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SafeGuard.Mobile.Models
{
    public class ContactModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _iAllowedThem;
        [JsonPropertyName("iAllowedThem")]
        public bool IAllowedThem
        {
            get => _iAllowedThem;
            set
            {
                if (_iAllowedThem != value)
                {
                    _iAllowedThem = value;
                    OnPropertyChanged(); 
                }
            }
        }

        private bool _theyAllowedMe;
        [JsonPropertyName("theyAllowedMe")]
        public bool TheyAllowedMe
        {
            get => _theyAllowedMe;
            set
            {
                if (_theyAllowedMe != value)
                {
                    _theyAllowedMe = value;
                    OnPropertyChanged(); 
                }
            }
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("bloodType")]
        public string BloodType { get; set; }

        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("isSosActive")]
        public bool IsSosActive { get; set; }

        [JsonPropertyName("lastStatusUpdate")]
        public DateTime? LastStatusUpdate { get; set; }

        [JsonPropertyName("medicalConditions")]
        public string MedicalConditions { get; set; }

        [JsonPropertyName("allergies")]
        public string Allergies { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("medications")]
        public string Medications { get; set; }

        [JsonPropertyName("organStatus")]
        public string OrganStatus { get; set; }

        [JsonPropertyName("alcoholUse")]
        public string AlcoholUse { get; set; }

        [JsonPropertyName("smokingHabit")]
        public string SmokingHabit { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("weight")]
        public int? Weight { get; set; }

        [JsonPropertyName("surgeries")]
        public string Surgeries { get; set; }

        [JsonPropertyName("organDetails")]
        public string OrganDetails { get; set; }
    }
}