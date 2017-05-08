using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("UniqueMaps")]
    public class UniqueMap
    {

        public string Name { get; set; }
        public string MapLevel { get; set; } //using string for future proofing
        public string Stats { get; set; }
        public string ImageUrl { get; set; } = "N/A";

    }

}
