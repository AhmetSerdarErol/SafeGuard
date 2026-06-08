using SafeGuard.Mobile.Models;

namespace SafeGuard.Mobile;

public partial class MedicalIdPage : ContentPage
{
    public MedicalIdPage(ContactModel contact)
    {
        InitializeComponent();

        if (contact != null)
        {
            IdNameLabel.Text = contact.Name;
            IdBloodLabel.Text = string.IsNullOrWhiteSpace(contact.BloodType) ? "Bilinmiyor" : contact.BloodType;

            if (!string.IsNullOrWhiteSpace(contact.BirthDate) && contact.BirthDate.Contains(" "))
                IdBirthLabel.Text = contact.BirthDate.Split(' ')[0];
            else
                IdBirthLabel.Text = string.IsNullOrWhiteSpace(contact.BirthDate) ? "-" : contact.BirthDate;

            IdHeightLabel.Text = contact.Height.HasValue ? $"{contact.Height} cm" : "-";
            IdWeightLabel.Text = contact.Weight.HasValue ? $"{contact.Weight} kg" : "-";
            IdMedicalLabel.Text = string.IsNullOrWhiteSpace(contact.MedicalConditions) ? "Sisteme Kayıtlı Değil" : contact.MedicalConditions;
            IdAllergyLabel.Text = string.IsNullOrWhiteSpace(contact.Allergies) ? "Sisteme Kayıtlı Değil" : contact.Allergies;
            IdMedicationsLabel.Text = string.IsNullOrWhiteSpace(contact.Medications) ? "Sisteme Kayıtlı Değil" : contact.Medications;
            IdSurgeriesLabel.Text = string.IsNullOrWhiteSpace(contact.Surgeries) ? "Sisteme Kayıtlı Değil" : contact.Surgeries;
            IdSmokeLabel.Text = string.IsNullOrWhiteSpace(contact.SmokingHabit) ? "Bilinmiyor" : contact.SmokingHabit;
            IdSmokeLabel.TextColor = (!string.IsNullOrWhiteSpace(contact.SmokingHabit) && contact.SmokingHabit.ToLower().Contains("kullan")) ? Colors.Red : Colors.LightGreen;
            IdAlcoholLabel.Text = string.IsNullOrWhiteSpace(contact.AlcoholUse) ? "Bilinmiyor" : contact.AlcoholUse;
            IdAlcoholLabel.TextColor = (!string.IsNullOrWhiteSpace(contact.AlcoholUse) && contact.AlcoholUse.ToLower().Contains("kullan")) ? Colors.Red : Colors.LightGreen;

            string organStatus = string.IsNullOrWhiteSpace(contact.OrganStatus) ? "Belirtilmemiş" : contact.OrganStatus;
            string organDetails = string.IsNullOrWhiteSpace(contact.OrganDetails) ? "" : $" ({contact.OrganDetails})";
            IdHabitsLabel.Text = organStatus + organDetails;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}