using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Models
{
    public class Card
    {
        public string CardName { get; set; }
        public string SetNumber { get; set; }
        public string Rarity { get; set; }
        public double? Price { get; set; }
    }
}
