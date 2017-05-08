using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("UniqueJewels")]
    public class UniqueJewel
    {

        public string Name { get; set; }
        public string Limit { get; set; }
        public string Radius { get; set; }
        public string Stats { get; set; }
        public bool IsCorrupted { get; set; } = false;
        public string ImageUrl { get; set; } = "N/A";
        public string ObtainMethod { get; set; }

    }

}
