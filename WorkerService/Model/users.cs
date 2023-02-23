using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace WorkerService.Model
{
    public class users
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public int id { get; set; }

        public string name { get; set; }

        public string password { get; set; }

        public string username { get; set; }
    }
}