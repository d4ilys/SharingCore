using System;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore.Assemble.Model
{
    public class DbWarp
    {
        /// <summary>
        /// 数据库名称，连接字符串中的Key
        /// </summary>
        public string Name { get; set; }

        public IFreeSql Instance { get; set; }
    }
}