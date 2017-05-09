using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{
    public class GenericFlask
    {

        public string Name { get; set; }

        public string LevelReq { get; set; } = "0";
        public string Life { get; set; } = "0";
        public string Mana { get; set; } = "0";
        public string Duration { get; set; } = "0";
        public string Usage { get; set; } = "0";
        public string Capacity { get; set; } = "0";
        public string ImageUrl { get; set; } = "N/A";
        public string BuffEffects { get; set; } = "N/A";
        public string Stats { get; set; } = "N/A";
        public string Type { get; set; }
        public string Url { get; set; } = "N/A";

    }
}
