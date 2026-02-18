namespace SafeGuard.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            if (Preferences.ContainsKey("CurrentUserId"))
            {
                // Giriş yapmışsa direkt Dashboard'a (veya AppShell'e) gönder
                MainPage = new AppShell();
            }
            else
            {
                // Giriş yapmamışsa senin Login ekranın olan MainPage'e gönder
                MainPage = new NavigationPage(new MainPage());
            }
        }
    }
}