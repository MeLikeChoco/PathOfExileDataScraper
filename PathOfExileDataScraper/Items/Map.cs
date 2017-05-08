using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("Maps")]
    public class Map
    {

        public string Name { get; set; }
        public int MapLevel { get; set; }
        public string Tier { get; set; }
        public bool Unique { get; set; }
        public string LayoutType { get; set; }
        public string BossDifficulty { get; set; }
        public string LayoutSet { get; set; }
        public string UniqueBoss { get; set; }
        public int NumberOfUniqueBosses { get; set; }
        public int SextantCoverage { get; set; }
        public string ImageUrl { get; set; }

    }

}
