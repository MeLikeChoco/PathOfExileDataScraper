using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
{
    public class GenericFlask
    {

        public string Name { get; set; }

        public int LevelReq { get; set; } = 0;
        public int Life { get; set; } = 0;
        public int Mana { get; set; } = 0;
        public double Duration { get; set; } = 0.0;
        public int Usage { get; set; } = 0;
        public int Capacity { get; set; } = 0;
        public string ImageUrl { get; set; } = "N/A";
        public string BuffEffects { get; set; } = "N/A";
        public string Stats { get; set; } = "N/A";
        public string Type { get; set; }

    }
}
