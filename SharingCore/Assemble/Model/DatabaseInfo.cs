﻿using System;
using System.Collections.Generic;
using System.Text;
using FreeSql;

namespace FreeSql.SharingCore
{
    public class DatabaseInfo
    {
        /// <summary>
        /// Key就是区分唯一的标识，Key格式{数据库前缀(Identification)}_租户标识_分库标识
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// 数据库名称前缀，例如分库：Business_tenant01_2022，这里的Identificatione应该是Business
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
    }

    public class SeparateRule
    {
        public string Name { get; set; }

        public string Template { get; set; }

        /// <summary>
        /// 分库规则
        /// </summary>
        public string? Separate { get; set; }
    }

    public class HorizontalShardingRule
    {
        public string Name { get; set; }

        public int DatabaseCount { get; set; }

        public int ShardingCount { get; set; }
    }

    public class SharingCoreDbConfig
    {
        /// <summary>
        /// 是否显示SQL
        /// </summary>
        public bool ShowSqlLog { get; set; } = true;

        public List<DatabaseInfo> DatabaseInfo { get; set; }

        public List<SeparateRule> SeparateRules { get; set; } = new List<SeparateRule>();
        public List<HorizontalShardingRule> HorizontalShardingRules { get; set; } = new List<HorizontalShardingRule>();
    }
}