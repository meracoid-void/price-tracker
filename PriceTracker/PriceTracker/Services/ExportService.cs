using PriceTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Services
{
    public class ExportService
    {
        private readonly IFileService _fileService;

        public ExportService(IFileService fileService)
        {
            _fileService = fileService;
        }
        public async Task<string> ExportAccountToCsvAsync(Account account)
        {
            var lines = new List<string>
            {
                $"Account:,{account.Name}",
                $"Credit:,{account.Credit}",
                "",
                "Binder Cards:",
                "Card Name,Set Number,Rarity,Price"
            };

            foreach (var card in account.InBinder)
            {
                lines.Add($"{card.CardName},{card.SetNumber},{card.Rarity},{card.Price}");
            }

            var fileName = $"{account.Name}_export.csv";
            var folderPath = _fileService.GetDownloadsPath();
            var fullPath = Path.Combine(folderPath, fileName);

            await File.WriteAllLinesAsync(fullPath, lines);

            return fullPath;
        }

        public async Task<string> ExportAllAccountsToCsvAsync(List<Account> accounts)
        {
            var lines = new List<string>
            {
                "Account Name,Credit,Card Count"
            };

            foreach (var account in accounts)
            {
                lines.Add($"{account.Name},{account.Credit},{account.InBinder?.Count ?? 0}");
            }

            var fileName = $"AllAccounts_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = _fileService.GetDownloadsPath();
            var fullPath = Path.Combine(path, fileName);
            await File.WriteAllLinesAsync(fullPath, lines);
            return path;
        }
    }
}
