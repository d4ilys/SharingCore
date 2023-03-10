using System;
using System.Collections.Generic;
using System.Text;
using FreeSql;

namespace SharingCore.Assemble.Model
{
    public class DbConfig
    {
        /// <summary>
        /// Key就是区分唯一的标识，Key格式{数据库前缀(Identification)}_租户标识_分库标识
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// 数据库名称前缀，例如分库：Business_tenant01_2022，这里的KIdentificationey应该是Business
        /// </summary>
        public string? Identification { get; set; }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string? ConnectString { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 读写分离-从库
        /// </summary>
        public List<string> Slaves { get; set; } = new List<string>();

        /// <summary>
        /// 是否加入FreeSql全局过滤器
        /// </summary>
        public bool IsFilter { get; set; } = false;
    }
}