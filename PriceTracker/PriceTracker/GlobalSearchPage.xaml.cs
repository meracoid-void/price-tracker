using PriceTracker.Models;

namespace PriceTracker;

public partial class GlobalSearchPage : ContentPage
{
	public GlobalSearchPage()
	{
		InitializeComponent();
	}

    private void OnSearchClicked(object sender, EventArgs e)
    {
        string searchTerm = SearchEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            DisplayAlert("Error", "Please enter a card name.", "OK");
            return;
        }

        var results = new List<GlobalCardResult>();

        foreach (var account in AppData.Accounts)
        {
            var matches = account.InBinder
                .Where(card => card.CardName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(card => new GlobalCardResult
                {
                    CardName = card.CardName,
                    SetNumber = card.SetNumber,
                    Rarity = card.Rarity,
                    Owner = account.Name,
                    Price = card.Price ?? 0,
                    AccountRef = account,
                    CardRef = card
                });

            results.AddRange(matches);
        }

        SearchResultsView.ItemsSource = results;
    }

    private async void OnSellCardClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is GlobalCardResult result)
        {
            var account = result.AccountRef;
            var card = result.CardRef;

            // Step 1: Get sell price
            string amountStr = await DisplayPromptAsync("Sell Card", $"How much did you sell '{card.CardName}' for?", "OK", "Cancel", keyboard: Keyboard.Numeric);
            if (!double.TryParse(amountStr, out double sellAmount))
            {
                await DisplayAlert("Invalid", "Please enter a valid number.", "OK");
                return;
            }

            // Step 2: Choose buyer
            string[] buyerNames = AppData.Accounts.Where(a => a != account).Select(a => a.Name).ToArray();
            string chosenBuyer = await DisplayActionSheet("Did another account buy this card?", "No (Sold externally)", null, buyerNames);

            if (!string.IsNullOrWhiteSpace(chosenBuyer) && chosenBuyer != "No (Sold externally)")
            {
                var buyerAccount = AppData.Accounts.FirstOrDefault(a => a.Name == chosenBuyer);
                if (buyerAccount != null)
                {
                    buyerAccount.Credit -= sellAmount;

                    buyerAccount.BuyHistory ??= new List<Card>();
                    buyerAccount.BuyHistory.Add(new Card
                    {
                        CardName = card.CardName,
                        SetNumber = card.SetNumber,
                        Rarity = card.Rarity,
                        Price = sellAmount
                    });
                }
            }

            // Step 3: Move to SellHistory
            account.SellHistory ??= new List<Card>();
            account.SellHistory.Add(new Card
            {
                CardName = card.CardName,
                SetNumber = card.SetNumber,
                Rarity = card.Rarity,
                Price = sellAmount
            });

            // Step 4: Update credit and remove from binder
            account.Credit = (account.Credit ?? 0) + sellAmount;
            account.InBinder.Remove(card);

            // Step 5: Save
            await AppData.SaveAccounts.Invoke();

            // Step 6: Refresh view
            OnSearchClicked(null, null); // refresh search result
        }
    }
}