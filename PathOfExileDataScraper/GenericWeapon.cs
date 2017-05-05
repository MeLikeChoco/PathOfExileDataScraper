using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
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
        public double APS { get; set; } = 0;
        public double CritChance { get; set; } = 0;
        public double DPS { get; set; } = 0;
        public string Stats { get; set; } = "N/A";
        public string ImageUrl { get; set; }

    }

}
