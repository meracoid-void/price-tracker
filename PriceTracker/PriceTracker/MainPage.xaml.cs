using PriceTracker.Models;
using PriceTracker.Services;
using System.Text.Json;

namespace PriceTracker
{
    public partial class MainPage : ContentPage
    {
        private static readonly string FilePath = Path.Combine(FileSystem.AppDataDirectory, "accounts.json");
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

            RefreshAccountList();

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

        private async void OnViewAccountClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Account account)
            {
                await Navigation.PushAsync(new AccountDetailPage(account, _exportService));
            }
        }

        private async void OnDeleteAccountClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Account account)
            {
                bool confirm = await DisplayAlert("Delete Account",
                    $"Are you sure you want to delete '{account.Name}'?",
                    "Yes", "Cancel");

                if (!confirm) return;

                AppData.Accounts.Remove(account);
                await AppData.SaveAccounts.Invoke();
                RefreshAccountList();
            }
        }

        private void RefreshAccountList()
        {
            AccountsListView.ItemsSource = null;
            AccountsListView.ItemsSource = AppData.Accounts;
        }

        private async void OnGlobalSearchClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GlobalSearchPage());
        }
    }
}
