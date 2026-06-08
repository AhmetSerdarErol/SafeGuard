using Plugin.Maui.Audio;

namespace SafeGuard.Mobile.Views;

public partial class FakeCallPage : ContentPage
{
    private System.Timers.Timer _callTimer;
    private int _secondsElapsed = 0;
    private IAudioPlayer _audioPlayer;

    public FakeCallPage()
    {
        InitializeComponent();
        PlayRingtone(); 
    }

    private async void PlayRingtone()
    {
        try
        {
            var audioManager = AudioManager.Current;
            
            _audioPlayer = audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("ringtone.mp3"));
            _audioPlayer.Loop = true; 
            _audioPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ses çalınırken hata oluştu: {ex.Message}");
        }
    }

    private async void OnDeclineClicked(object sender, EventArgs e)
    {
        _audioPlayer?.Stop(); 

        if (_callTimer != null)
        {
            _callTimer.Stop();
            _callTimer.Dispose();
        }
        await Navigation.PopModalAsync(); 
    }

    private void OnAcceptClicked(object sender, EventArgs e)
    {
        _audioPlayer?.Stop(); 

        TimerLabel.IsVisible = true;
        AcceptContainer.IsVisible = false;

        _callTimer = new System.Timers.Timer(1000);
        _callTimer.Elapsed += (s, args) =>
        {
            _secondsElapsed++;
            var timeString = TimeSpan.FromSeconds(_secondsElapsed).ToString(@"mm\:ss");
            MainThread.BeginInvokeOnMainThread(() => TimerLabel.Text = timeString);
        };
        _callTimer.Start();
    }
}