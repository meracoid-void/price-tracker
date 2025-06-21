using PriceTracker.Models;
namespace PriceTracker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnSleep()
        {
            AppData.SaveAccounts?.Invoke();
        }
    }
}