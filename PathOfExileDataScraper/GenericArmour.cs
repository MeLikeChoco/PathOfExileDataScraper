﻿using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
{

    [Table("GenericArmours")]
    public class GenericArmour
    {
        
        public string Name { get; set; }

        public int LevelReq { get; set; } = 0;
        public int Strength { get; set; } = 0;
        public int Dexterity { get; set; } = 0;
        public int Intelligence { get; set; } = 0;
        public int Armour { get; set; } = 0;
        public int Evasion { get; set; } = 0;
        public int EnergyShield { get; set; } = 0;
        public string BlockChance { get; set; } = "0%";
        public string ImageUrl { get; set; }
        public string Stats { get; set; } = "N/A";
        public string Type { get; set; }

    }

}