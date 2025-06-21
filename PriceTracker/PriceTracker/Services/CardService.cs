using PriceTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PriceTracker.Services
{
    public class CardService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, TcgPlayerData> _cache = new();

        public CardService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<TcgPlayerData?> GetCardDataAsync(string cardName)
        {
            string key = cardName.ToLowerInvariant();
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            try
            {
                string url = $"https://db.ygoprodeck.com/api/v7/cardinfo.php?name={Uri.EscapeDataString(cardName)}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<TcgPlayer>(json);
                var result = data?.data?.FirstOrDefault();

                if (result != null)
                    _cache[key] = result;

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
