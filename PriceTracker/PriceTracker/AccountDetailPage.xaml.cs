using PriceTracker.Models;
using PriceTracker.Services;
using System.Text.Json;

namespace PriceTracker
{
    public partial class AccountDetailPage : ContentPage
    {
        private readonly CardService _cardService = new();
        private Account _account;
        private ExportService _exportService;
        private ITextRecognitionService _ocrService;

        public AccountDetailPage(Account account, ExportService exportService, ITextRecognitionService ocrService)
        {
            InitializeComponent();
            _account = account;
            BindingContext = _account;

            NameLabel.Text = $"Name: {_account.Name}";
            CreditLabel.Text = $"Credit: ${_account.Credit ?? 0:F2}";

            BinderListView.ItemsSource = _account.InBinder;

            _exportService = exportService;
            _ocrService = ocrService;

            _ = UpdateCardPricesAsync(); // fire-and-forget
        }

        private void RefreshBinder()
        {
            CardNameEntry.Text = string.Empty;
            BinderListView.ItemsSource = null;
            BinderListView.ItemsSource = _account.InBinder;
            NameLabel.Text = $"Name: {_account.Name}";
            CreditLabel.Text = $"Credit: ${_account.Credit ?? 0:F2}";
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

            string qtyInput = await DisplayPromptAsync("Quantity", "How many copies do you want to add?", "Add", "Cancel", "1", keyboard: Keyboard.Numeric);
            if (!int.TryParse(qtyInput, out int quantity) || quantity <= 0)
            {
                quantity = 1;
            }

            for (int i = 0; i < quantity; i++)
            {
                _account.InBinder.Add(newCard);
            }
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

        private async void OnRemoveCardClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Card card)
            {
                bool confirm = await DisplayAlert("Remove Card", $"Are you sure you want to remove '{card.CardName}'?", "Yes", "Cancel");
                if (!confirm) return;

                _account.InBinder.Remove(card);
                RefreshBinder();

                if (AppData.SaveAccounts != null)
                    await AppData.SaveAccounts.Invoke();
            }
        }

        private async void OnExportAccountClicked(object sender, EventArgs e)
        {
            var filePath = await _exportService.ExportAccountToCsvAsync(_account);
            await DisplayAlert("Exported", $"Saved to Downloads:\n{filePath}", "OK");
        }

        private async void OnAddCreditClicked(object sender, EventArgs e)
        {
            string amountStr = await DisplayPromptAsync("Add Credit", "How much credit to add?", "OK", "Cancel", keyboard: Keyboard.Numeric);

            if (!double.TryParse(amountStr, out double amount) || amount <= 0)
            {
                await DisplayAlert("Invalid Input", "Please enter a positive number.", "OK");
                return;
            }

            _account.Credit = (_account.Credit ?? 0) + amount;
            CreditLabel.Text = $"Credit: ${_account.Credit:F2}";

            if (AppData.SaveAccounts != null)
            {
                await AppData.SaveAccounts.Invoke();
            }
        }

        private async void OnRemoveCreditClicked(object sender, EventArgs e)
        {
            string amountStr = await DisplayPromptAsync("Remove Credit", "How much credit to subtract?", "OK", "Cancel", keyboard: Keyboard.Numeric);

            if (!double.TryParse(amountStr, out double amount) || amount <= 0)
            {
                await DisplayAlert("Invalid Input", "Please enter a positive number.", "OK");
                return;
            }

            _account.Credit = (_account.Credit ?? 0) - amount;
            CreditLabel.Text = $"Credit: ${_account.Credit:F2}";

            if (AppData.SaveAccounts != null)
            {
                await AppData.SaveAccounts.Invoke();
            }
        }

        private async void OnBuyFromShopClicked(object sender, EventArgs e)
        {
            string cardName = await DisplayPromptAsync("Buy Card", "Enter card name: (Optional)");
            if (!String.IsNullOrEmpty(cardName))
            {
                // Call your API fetch method
                var apiResult = await _cardService.GetCardDataAsync(cardName);
                if (apiResult == null || apiResult.card_sets.Count == 0)
                {
                    await DisplayAlert("Error", "Card not found.", "OK");
                    return;
                }

                // Let user choose the version/set
                var choices = apiResult.card_sets.Select(set => $"{set.set_code} - {set.set_rarity}").ToArray();
                string chosen = await DisplayActionSheet("Select Card Version", "Cancel", null, choices);
                if (chosen == null || chosen == "Cancel")
                {
                    return;
                }

                // Prompt for purchase price
                string inputPrice = await DisplayPromptAsync("Price Paid", "How much did this cost?", "Submit", keyboard: Keyboard.Numeric);
                if (!double.TryParse(inputPrice, out double paid)) paid = 0.0;

                var selectedSet = apiResult.card_sets.FirstOrDefault(set =>
                    $"{set.set_code} - {set.set_rarity}" == chosen);

                // Add to binder and buy history
                var chosenCard = new Card()
                {
                    CardName = cardName,
                    SetNumber = selectedSet.set_code,
                    Price = paid,
                    Rarity = selectedSet.set_rarity
                };
                _account.BuyHistory.Add(chosenCard);

                // Subtract credit
                _account.Credit -= paid;
            }
            else
            {
                string inputPrice = await DisplayPromptAsync("Price Paid", "How much did this cost?", "Submit", keyboard: Keyboard.Numeric);
                if (!double.TryParse(inputPrice, out double paid)) paid = 0.0;
                var chosenCard = new Card()
                {
                    CardName = "N/A",
                    SetNumber = "N/A",
                    Price = paid,
                    Rarity = "N/A"
                };
                _account.BuyHistory.Add(chosenCard);

                // Subtract credit
                _account.Credit -= paid;
            }


                await AppData.SaveAccounts.Invoke();
            RefreshBinder();
        }

        private async void OnScanCardWithCameraClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo == null)
                    return;

                using var stream = await photo.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var scannedText = await _ocrService.RecognizeTextAsync(bytes);

                if (string.IsNullOrWhiteSpace(scannedText))
                {
                    await DisplayAlert("No Text Found", "Could not detect text from image.", "OK");
                    return;
                }

                // Try to clean up OCR result � use first line only
                var firstLine = scannedText.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    await DisplayAlert("Invalid Scan", "Couldn't read a valid card name.", "OK");
                    return;
                }

                // Optionally: Ask user to confirm or edit
                string confirmedCardName = await DisplayPromptAsync("Confirm Card Name", "Edit or confirm the scanned card name:", initialValue: firstLine);
                if (string.IsNullOrWhiteSpace(confirmedCardName))
                    return;

                // Use your existing flow to fetch card from YGO API
                CardNameEntry.Text = confirmedCardName;
                OnAddCardClicked(null, EventArgs.Empty); // reuse existing add logic
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}