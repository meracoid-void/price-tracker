using PriceTracker.Models;
using PriceTracker.Services;
using System.Text.Json;

namespace PriceTracker
{
    public partial class AccountDetailPage : ContentPage
    {
        private readonly CardService _cardService = new();
        private Account _account;

        public AccountDetailPage(Account account)
        {
            InitializeComponent();
            _account = account;
            BindingContext = _account;

            NameLabel.Text = $"Name: {_account.Name}";
            CreditLabel.Text = $"Credit: ${_account.Credit ?? 0:F2}";

            BinderListView.ItemsSource = _account.InBinder;

            _ = UpdateCardPricesAsync(); // fire-and-forget
        }

        private void RefreshBinder()
        {
            CardNameEntry.Text = string.Empty;
            BinderListView.ItemsSource = null;
            BinderListView.ItemsSource = _account.InBinder;
        }

        private async void OnAddCardClicked(object sender, EventArgs e)
        {
            string cardName = CardNameEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(cardName))
            {
                await DisplayAlert("Invalid Input", "Card name cannot be empty.", "OK");
                return;
            }

            var apiCard = await _cardService.GetCardDataAsync(cardName);
            if (apiCard == null)
                return;

            // No sets? Add with default info
            if (apiCard.card_sets == null || apiCard.card_sets.Count == 0)
            {
                _account.InBinder.Add(new Card
                {
                    CardName = apiCard.name,
                    SetNumber = "N/A",
                    Rarity = "N/A"
                });

                RefreshBinder();
                return;
            }

            // Create display choices
            var choices = apiCard.card_sets.Select(set => $"{set.set_code} - {set.set_rarity}").ToArray();

            // Let user choose
            string chosen = await DisplayActionSheet("Select Card Version", "Cancel", null, choices);
            if (chosen == null || chosen == "Cancel")
                return;

            var selectedSet = apiCard.card_sets.FirstOrDefault(set =>
                $"{set.set_code} - {set.set_rarity}" == chosen);

            if (selectedSet == null)
                return;

            var price = Double.Parse(selectedSet.set_price);

            var newCard = new Card
            {
                CardName = apiCard.name,
                SetNumber = selectedSet.set_code,
                Rarity = selectedSet.set_rarity,
                Price = price
            };

            _account.InBinder.Add(newCard);
            RefreshBinder();

            if (AppData.SaveAccounts != null)
                await AppData.SaveAccounts.Invoke();
        }


        private async Task UpdateCardPricesAsync()
        {
            foreach (var card in _account.InBinder)
            {
                try
                {
                    string encodedName = Uri.EscapeDataString(card.CardName);
                    
                    var ygoCard = await _cardService.GetCardDataAsync(encodedName);
                    if (ygoCard == null || ygoCard.card_sets == null)
                        continue;

                    var matchingSet = ygoCard.card_sets.FirstOrDefault(cs => cs.set_code == card.SetNumber);
                    if (matchingSet == null)
                        continue;

                    var price = Double.Parse(matchingSet.set_price);
                    card.Price = price;
                }
                catch
                {
                    // You can log or ignore errors individually
                    card.Price = 0;
                }
            }

            RefreshBinder();
        }

        private async void OnSellCardClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Card cardToSell)
            {
                // Step 1: Get sell price
                string amountStr = await DisplayPromptAsync("Sell Card", $"How much did you sell '{cardToSell.CardName}' for?", "OK", "Cancel", keyboard: Keyboard.Numeric);
                if (!double.TryParse(amountStr, out double sellAmount))
                {
                    await DisplayAlert("Invalid", "Please enter a valid number.", "OK");
                    return;
                }

                // Step 2: Choose buyer
                string[] buyerNames = AppData.Accounts.Where(a => a != _account).Select(a => a.Name).ToArray();
                string chosenBuyer = await DisplayActionSheet("Did another account buy this card?", "No (Sold externally)", null, buyerNames);

                // Step 3: Update buyer if internal sale
                if (!string.IsNullOrWhiteSpace(chosenBuyer) && chosenBuyer != "No (Sold externally)")
                {
                    var buyerAccount = AppData.Accounts.FirstOrDefault(a => a.Name == chosenBuyer);
                    if (buyerAccount != null)
                    {
                        buyerAccount.Credit -= sellAmount;

                        buyerAccount.BuyHistory ??= new List<Card>();
                        buyerAccount.BuyHistory.Add(new Card
                        {
                            CardName = cardToSell.CardName,
                            SetNumber = cardToSell.SetNumber,
                            Rarity = cardToSell.Rarity,
                            Price = sellAmount
                        });
                    }
                }

                // Step 4: Add to seller's SellHistory
                _account.SellHistory ??= new List<Card>();
                _account.SellHistory.Add(new Card
                {
                    CardName = cardToSell.CardName,
                    SetNumber = cardToSell.SetNumber,
                    Rarity = cardToSell.Rarity,
                    Price = sellAmount
                });

                // Step 5: Update credit
                _account.Credit = (_account.Credit ?? 0) + sellAmount;

                // Step 6: Remove from binder
                _account.InBinder.Remove(cardToSell);

                RefreshBinder();

                // Step 7: Save changes if needed
                AppData.SaveAccounts?.Invoke(); // optional hook to persist
            }
        }

        private async void OnViewHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AccountHistoryPage(_account));
        }
    }
}