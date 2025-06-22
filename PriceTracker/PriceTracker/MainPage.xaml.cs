using PriceTracker.Models;
using PriceTracker.Services;
using System.Text.Json;

namespace PriceTracker
{
    public partial class MainPage : ContentPage
    {
        private static readonly string FilePath = Path.Combine(FileSystem.AppDataDirectory, "accounts.json");
        private List<Account> _accounts;
        private ExportService _exportService;

        public MainPage(ExportService exportService)
        {
            InitializeComponent();
            _exportService = exportService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (AppData.Accounts.Count == 0)
            {
                var accounts = await LoadAccountsFromFileAsync(); // your file logic
                AppData.Accounts = accounts ?? new List<Account>();

                // Assign save method
                AppData.SaveAccounts = async () => await SaveAccountsToFileAsync(AppData.Accounts);
            }

            AccountsListView.ItemsSource = null;
            AccountsListView.ItemsSource = AppData.Accounts;

            AppData.SaveAccounts = async () => await SaveAccountsToFileAsync(AppData.Accounts);

            // Start autosave every 30 seconds
            StartAutoSaveTimer();
        }

        private CancellationTokenSource _cts;

        private void StartAutoSaveTimer()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);

                        if (_cts.Token.IsCancellationRequested)
                            break;

                        if (AppData.SaveAccounts != null)
                            await AppData.SaveAccounts.Invoke();
                    }
                    catch (TaskCanceledException)
                    {
                        // Timer was cancelled, exit gracefully
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log error
                    }
                }
            }, _cts.Token);
        }

        //private void StopAutoSaveTimer()
        //{
        //    _cts?.Cancel();
        //}

        public static async Task<List<Account>> LoadAccountsFromFileAsync()
        {
            if (!File.Exists(FilePath))
                return new List<Account>();

            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<Account>>(json);
        }

        public static async Task SaveAccountsToFileAsync(List<Account> accounts)
        {
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
        }

        private async void OnAddAccountClicked(object sender, EventArgs e)
        {
            string name = AccountNameEntry.Text?.Trim();
            string creditText = StartingCreditEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Invalid Input", "Account name cannot be empty.", "OK");
                return;
            }

            if (!double.TryParse(creditText, out double startingCredit))
            {
                await DisplayAlert("Invalid Input", "Starting credit must be a valid number. Will set credit to 0", "OK");
                startingCredit = 0;
            }

            var newAccount = new Account
            {
                Name = name,
                Credit = startingCredit,
                InBinder = new List<Card>(),
                BuyHistory = new List<Card>(),
                SellHistory = new List<Card>()
            };

            AppData.Accounts.Add(newAccount);

            if (AppData.SaveAccounts != null)
                await AppData.SaveAccounts.Invoke();

            AccountsListView.ItemsSource = null;
            AccountsListView.ItemsSource = AppData.Accounts;

            AccountNameEntry.Text = string.Empty;
            StartingCreditEntry.Text = string.Empty;
        }

        void OnAccountSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Account selectedAccount)
            {
                // Navigate to detail page
                Navigation.PushAsync(new AccountDetailPage(selectedAccount, _exportService));
                AccountsListView.SelectedItem = null; // clear selection
            }
        }
    }
}
