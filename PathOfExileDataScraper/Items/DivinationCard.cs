using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("DivinationCards")]
    public class DivinationCard
    {

        public string Name { get; set; }
        public int Set { get; set; }
        public string Reward { get; set; }
        public bool IsRewardCorrupted { get; set; } = false;
        public string DropRestrictions { get; set; }
        public string ImageUrl { get; set; } = "N/A";
        public string Url { get; set; } = "N/A";

    }

}
