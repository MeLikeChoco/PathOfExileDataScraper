using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper.Items
{

    [Table("UniqueWeapons")]
    public class UniqueWeapon : GenericWeapon
    {
        
        public string PDPS { get; set; } = "N/A";
        public string EDPS { get; set; } = "N/A";

    }
}
