using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Models
{
    public static class AppData
    {
        public static List<Account> Accounts { get; set; } = new();
        public static Func<Task> SaveAccounts { get; set; }  // hook for persisting
    }
    public class Account
    {
        private string _name;
        private double? _credit;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double? Credit
        {
            get => _credit;
            set => SetProperty(ref _credit, value);
        }

        public List<Card> InBinder { get; set; } = new();
        public List<Card> BuyHistory { get; set; } = new();
        public List<Card> SellHistory { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
