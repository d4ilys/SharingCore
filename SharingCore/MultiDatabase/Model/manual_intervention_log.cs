using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using FreeSql.DataAnnotations;

namespace SharingCore.MultiDatabase.Model
{
    public class manual_intervention_log
    {
        [Column(IsIdentity = true)] public BigInteger id { get; set; }

        /// <summary>
        /// 日志id
        /// </summary>
        public BigInteger log_id { get; set; }

        public DateTime create_time { get; set; }
    }
}