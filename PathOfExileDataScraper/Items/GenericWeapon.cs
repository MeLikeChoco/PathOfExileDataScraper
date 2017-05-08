using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("GenericWeapons")]
    public class GenericWeapon
    {

        public string Name { get; set; }

        public int LevelReq { get; set; } = 0;
        public int Strength { get; set; } = 0;
        public int Dexterity { get; set; } = 0;
        public int Intelligence { get; set; } = 0;
        public string Damage { get; set; } = "0";
        public string APS { get; set; } = "0";
        public string CritChance { get; set; } = "0%";
        public string DPS { get; set; } = "0";
        public string Stats { get; set; } = "N/A";
        public string ImageUrl { get; set; }
        public string Type { get; set; } = "N/A";

    }

}
