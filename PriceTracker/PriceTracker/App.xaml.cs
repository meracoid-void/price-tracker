using PriceTracker.Models;
using PriceTracker.Services;
namespace PriceTracker
{
    public partial class App : Application
    {
        private ExportService _exportService;
        public App(ExportService exportService)
        {
            InitializeComponent();
            _exportService = exportService;
            MainPage = new NavigationPage(new MainPage(_exportService));
        }

        protected override void OnSleep()
        {
            AppData.SaveAccounts?.Invoke();
        }
    }
}