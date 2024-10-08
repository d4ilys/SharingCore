﻿using FreeSql;
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
        /// FreeSql对象初始化完成后执行动作
        /// </summary>
        public List<Action>? InitializationCompleteExecutor { get; set; } = new List<Action>();

        /// <summary>
        /// 是否根据SharingCoreDbs中的扩展方法按需加载数据库
        /// </summary>
        public bool DemandLoading { get; set; } = false;

        /// <summary>
        /// 开启 ADO.NET 自带的连接池
        /// </summary>
        public bool UseAdoConnectionPool { get; set; } = true;

        /// <summary>
        /// 是否监听配置文件自动初始化数据库
        /// </summary>
        public bool ListeningConfiguration { get; set; } = false;

        /// <summary>
        /// FreeSql 全局 Aop
        /// </summary>
        public Action<IFreeSql>? Aop { get; set; } = null;

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


        internal List<dynamic?> FreeSqlFilterExpression = new List<dynamic?>();

        internal List<ApplyIfFilterDescribe> FreeSqlIfFilterExpression = new List<ApplyIfFilterDescribe>();

        /// <summary>
        /// FreeSql Apply 全局过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        public void ApplyFilter<T>(Expression<Func<T, bool>> expression)
        {
            FreeSqlFilterExpression.Add(expression);
        }

        /// <summary>
        /// FreeSql Apply 全局过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="condition"></param>
        /// <param name="expression"></param>
        /// <param name="before"></param>
        public void ApplyIfFilter<T>(Func<bool> condition, Expression<Func<T, bool>> expression, bool before = false)
        {
            FreeSqlIfFilterExpression.Add(new ApplyIfFilterDescribe
            {
                Condition = condition,
                Expression = expression,
                Before = before
            });
        }
    }

    internal class ApplyIfFilterDescribe
    {
        public Func<bool> Condition { get; set; }

        public dynamic? Expression { get; set; } = null;

        public bool Before { get; set; }
    }
}