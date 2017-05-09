using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("GenericArmours")]
    public class GenericArmour
    {
        
        public string Name { get; set; }

        public string LevelReq { get; set; } = "0";
        public string Strength { get; set; } = "0";
        public string Dexterity { get; set; } = "0";
        public string Intelligence { get; set; } = "0";
        public string Armour { get; set; } = "0";
        public string Evasion { get; set; } = "0";
        public string EnergyShield { get; set; } = "0";
        public string BlockChance { get; set; } = "N/A";
        public string ImageUrl { get; set; }
        public string Url { get; set; } = "N/A";
        public string Stats { get; set; } = "N/A";
        public string Type { get; set; }

    }

}
