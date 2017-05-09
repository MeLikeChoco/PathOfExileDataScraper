using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("Currencies")]
    public class Currency
    {

        public string Name { get; set; }
        public string DropLevel { get; set; }
        public string StackSize { get; set; } //string for future proofing
        public string TabStackSize { get; set; }
        public string HelpText { get; set; }
        public bool IsDiscontinued { get; set; }
        public string ImageUrl { get; set; } = "N/A";
        public string Url { get; set; } = "N/A";

    }

}
