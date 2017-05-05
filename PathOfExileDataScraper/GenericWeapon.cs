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

        [ExplicitKey]
        internal string Name { get; set; }
        internal int Level { get; set; }
        internal int Strength { get; set; } = 0;
        internal int Dexterity { get; set; } = 0;
        internal int Intelligence { get; set; } = 0;
        internal string Damage { get; set; }
        internal double APS { get; set; }
        internal double CritChance { get; set; }
        internal double DPS { get; set; }
        internal string Stats { get; set; } = "N/A";
        internal string ImageUrl { get; set; } = "N/A";

    }

}
