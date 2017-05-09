using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("Essences")]
    public class Essence
    {

        public string Name { get; set; }
        public string Tier { get; set; }
        public string DropLevel { get; set; }
        public string Effects { get; set; }
        public string ImageUrl { get; set; } = "N/A";
        public string Url { get; set; } = "N/A";

    }

}
