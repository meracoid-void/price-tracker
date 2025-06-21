using PriceTracker.Models;
namespace PriceTracker
{
    public partial class AccountHistoryPage : ContentPage
    {
        public AccountHistoryPage(Account account)
        {
            InitializeComponent();

            BuyHistoryListView.ItemsSource = account.BuyHistory ?? new List<Card>();
            SellHistoryListView.ItemsSource = account.SellHistory ?? new List<Card>();
        }
    }

}
