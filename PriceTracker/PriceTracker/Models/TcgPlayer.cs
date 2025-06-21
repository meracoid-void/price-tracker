using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Models
{
    public class TcgPlayer
    {
        public List<TcgPlayerData> data { get; set; }
    }

    public class TcgPlayerData
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<string> typeline { get; set; }
        public string type { get; set; }
        public string human_readable_card_type { get; set; }
        public string frame_type { get; set; }
        public string desc { get; set; }
        public string race { get; set; }
        public int atk { get; set; }
        public int def { get; set; }
        public int level { get; set; }
        public string attribute { get; set; }
        public string ygoprodeck_url { get; set; }
        public List<CardSet> card_sets { get; set; }
        public List<CardImage> card_images { get; set; }
        public List<CardPrice> card_prices { get; set; }
    }

    public class CardSet
    {
        public string set_name { get; set; }
        public string set_code { get; set; }
        public string set_rarity { get; set; }
        public string set_rarity_code { get; set; }
        public string set_price { get; set; }
    }

    public class CardImage
    {
        public int id { get; set; }
        public string image_url { get; set; }
        public string image_url_small { get; set; }
        public string image_url_cropped { get; set; }
    }

    public class CardPrice
    {
        public string cardmarket_price { get; set; }
        public string tcgplayer_price { get; set; }
        public string ebay_price { get; set; }
        public string amazon_price { get; set; }
        public string coolstuffinc_price { get; set; }
    }
}
