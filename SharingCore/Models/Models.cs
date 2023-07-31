using FreeSql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SharingCore
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
        public List<DatabaseInfo>? CustomDatabaseInfo { get; set; } = null;

        /// <summary>
        /// 是否根据SharingCoreDbs中的扩展方法按需加载数据库
        /// </summary>
        public bool DemandLoading { get; set; } = false;

        /// <summary>
        /// 父项目的Assembly，用于扫描SharingCoreDbs扩展方法
        /// </summary>
        public Assembly? BaseReferenceAssembly { get; set; } = null;

        /// <summary>
        /// 对不同数据库定制化设置
        /// </summary>
        public Dictionary<string, CustomDatabaseSettings>? CustomDatabaseSettings { get; set; } =
            new Dictionary<string, CustomDatabaseSettings>();

        /// <summary>
        /// 对所有库定制化设置，优先级低于单库设置
        /// </summary>
        public CustomDatabaseSettings? CustomAllDatabaseSettings { get; set; } = null;
    }

    public class CustomDatabaseSettings
    {
        /// <summary>
        /// 对FreeSqlBuilder扩展设置
        /// </summary>
        public Func<FreeSqlBuilder, FreeSqlBuilder>? FreeSqlBuilderInject { get; set; } = null;

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
