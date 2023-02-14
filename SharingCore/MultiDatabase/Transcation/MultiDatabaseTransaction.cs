using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SharingCore.Assemble.Model;

namespace SharingCore.MultiDatabase.Transcation
{
    public class MultiDatabaseTransaction
    {
        //使用AsyncLocal共享线程，提供异步方法版本
        public static DistributedTransaction Create(params DbWarp[] param)
        {
            var local = new AsyncLocal<DistributedTransaction>
            {
                Value = new DistributedTransaction(param.ToList())
            };
            return local.Value;
        }
    }
}
