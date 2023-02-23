using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace WorkerService.Model
{

    public class order
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public int id { get; set; }

        public string commodity_name { get; set; }

        public DateTime? order_time { get; set; }

        public string buyer_name { get; set; }
    }
}
