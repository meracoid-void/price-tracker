using PriceTracker.Models;
using PriceTracker.Services;
namespace PriceTracker
{
    public partial class App : Application
    {
        private ExportService _exportService;
        private ITextRecognitionService _ocrService;
        public App(ExportService exportService, ITextRecognitionService ocrService)
        {
            InitializeComponent();
            _exportService = exportService;
            _ocrService = ocrService;
            MainPage = new NavigationPage(new MainPage(_exportService, _ocrService));
        }

        protected override void OnSleep()
        {
            AppData.SaveAccounts?.Invoke();
        }
    }
}