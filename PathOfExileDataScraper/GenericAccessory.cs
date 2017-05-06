using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
{

    [Table("GenericAccessories")]
    public class GenericAccessory
    {
        
        public string Name { get; set; }

        public int LevelReq { get; set; } = 0;
        public string ImageUrl { get; set; } = "N/A";
        public string Stats { get; set; } = "N/A";
        public bool IsCorrupted { get; set; } = false;
        public string Type { get; set; }

    }

}
