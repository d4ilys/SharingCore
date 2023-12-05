using FreeSql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FreeSql.SharingCore
{
    public class SharingCoreOptions
    {
        /// <summary>
        /// 配置文件中数据库信息的KEY
        /// </summary>
        public string? DBConfigKey { get; set; } = string.Empty;

        /// <summary>
        /// 空闲过期时间
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 自定义的数据库信息
        /// </summary>
        public List<DatabaseInfo>? DatabaseInfoSource { get; set; } = null;

        /// <summary>
        /// 是否根据SharingCoreDbs中的扩展方法按需加载数据库
        /// </summary>
        public bool DemandLoading { get; set; } = false;

        /// <summary>
        /// 是否显示日志，优先级高于配置文件
        /// </summary>
        public bool ShowSqlLog { get; set; } = true;

        /// <summary>
        /// 指定项目的Assembly，用于扫描SharingCoreDbs扩展方法
        /// </summary>
        public List<Assembly> ExtensionMethodsAssembly { get; set; } = new List<Assembly>();

        /// <summary>
        /// 对不同数据库定制化设置
        /// </summary>
        public Dictionary<string, DatabaseOption>? DatabaseOptions { get; set; } =
            new Dictionary<string, DatabaseOption>();

        /// <summary>
        /// 对所有库定制化设置，优先级低于单库设置
        /// </summary>
        public DatabaseOption TogetherDatabaseOption { get; set; } = new DatabaseOption();
    }

    public class DatabaseOption
    {
        /// <summary>
        /// 对FreeSqlBuilder扩展设置
        /// </summary>
        public Func<FreeSqlBuilder, FreeSqlBuilder>? FreeSqlBuilderInject { get; set; } = null;

        /// <summary>
        /// 使用Ado自带的连接池
        /// </summary>
        public bool UseAdoConnectionPool { get; set; } = false;

        internal dynamic? FreeSqlFilterExpression = null;

        /// <summary>
        /// FreeSql 全局过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        public void FreeSqlFilter<T>(Expression<Func<T, bool>> expression)
        {
            FreeSqlFilterExpression = expression;
        }
    }
}