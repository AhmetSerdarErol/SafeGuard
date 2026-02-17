namespace SafeGuard.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // UYGULAMA NAVIGATION PAGE İLE BAŞLAMALI (Login Ekranı)
            MainPage = new NavigationPage(new MainPage());
        }
    }
}