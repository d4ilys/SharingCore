using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharingCore;

namespace WorkerService02
{
    public class FreeSqlFilter 
    {
        /// <summary>
        /// 是否删除
        /// </summary>
        public int IsDelete { get; set; }


        public int IsUpdate{ get; set; }
    }
}