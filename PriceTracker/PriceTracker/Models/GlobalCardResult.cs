using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Models
{
    public class GlobalCardResult
    {
        public string CardName { get; set; }
        public string SetNumber { get; set; }
        public string Rarity { get; set; }
        public string Owner { get; set; }
        public double Price { get; set; }

        public Account AccountRef { get; set; }
        public Card CardRef { get; set; }
    }
}
