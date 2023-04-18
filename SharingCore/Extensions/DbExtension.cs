using Microsoft.Extensions.DependencyInjection;
using SharingCore.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FreeSql;
using SharingCore.Assemble;
using SharingCore.Assemble.Model;

namespace SharingCore.Extensions
{
    public static class DbExtension
    {
        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options">自定义配置</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore(this IServiceCollection service,
            Action<SharingCoreOptions> options)
        {
            var configuration = SharingCoreUtils.Configuration;
            var option = new SharingCoreOptions();
            options?.Invoke(option);
            SharingCoreUtils.Options = option;
            IdleBusProvider.InitIdleBus(configuration, option);
            return service;
        }

        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="filter">全局过滤器，全库统一</param>
        /// <param name="options">自定义配置</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore<T>(this IServiceCollection service,
            Expression<Func<T, bool>> filter, Action<SharingCoreOptions> options)
        {
            var configuration = SharingCoreUtils.Configuration;
            var option = new SharingCoreOptions();
            options?.Invoke(option);
            SharingCoreUtils.Options = option;
            IdleBusProvider.InitIdleBus(configuration, filter, option);
            return service;
        }

        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="filters">全局过滤器，不同库不同过滤器，字典中设置Key为： FreeSqlFilterType.Communal 作用于所有库的过滤器</param>
        /// <param name="options">自定义配置</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore<T>(this IServiceCollection service,
            Dictionary<string, Expression<Func<T, bool>>> filters, Action<SharingCoreOptions> options)
        {
            var configuration = SharingCoreUtils.Configuration;
            var option = new SharingCoreOptions();
            options?.Invoke(option);
            SharingCoreUtils.Options = option;
            IdleBusProvider.InitIdleBus(configuration, filters, option);
            return service;
        }

        /// <summary>
        /// 直接通过数据库标识获取FreeSql对象
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="separateDbIdent">分库标识，默认没有分库标识</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static IFreeSql GetFreeSql(this string dbName, string separateDbIdent = "", string tenant = "")
        {
            return GetDbWarp(dbName, separateDbIdent, tenant).Instance;
        }

        /// <summary>
        /// 直接通过数据库标识获取FreeSql对象 - 当前年
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static IFreeSql GetNowFreeSql(this string dbName, string tenant = "")
        {
            return GetNowDbWarp(dbName, tenant).Instance;
        }

        /// <summary>
        /// 直接通过数据库标识获取DbWarp对象
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="separateDbIdent">分库标识，默认没有分库标识</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static DbWarp GetDbWarp(this string dbName, string separateDbIdent = "", string tenant = "")
        {
            return string.IsNullOrWhiteSpace(separateDbIdent)
                ? DbWarpFactory.Get(dbName, tenant)
                : DbWarpFactory.Get(dbName, separateDbIdent, tenant);
        }

        /// <summary>
        /// 直接通过数据库标识获取DbWarp对象 - 当前年
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static DbWarp GetNowDbWarp(this string dbName, string tenant = "")
        {
            var separate = SharingCoreUtils.TryGetDateTimeSeparate(dbName);
            var name = separate?.GetDbNameByColumnValue(DateTime.Now);
            return DbWarpFactory.GetByKey(name, tenant);
        }

        /// <summary>
        /// 根据日期判断表是否存在，如果不存在则创建。
        /// </summary>
        /// <param name="fsql"></param>
        /// <param name="type"></param>
        /// <param name="colnumValue"></param>
        public static void NonExistsTableBeCreateByColumnValue(this IFreeSql fsql, Type type, object colnumValue)
        {
            var tableName = fsql.CodeFirst.GetTableByEntity(type).AsTableImpl
                .GetTableNameByColumnValue(colnumValue);
            if (!fsql.DbFirst.ExistsTable(tableName))
            {
                fsql.CodeFirst.SyncStructure(type, tableName);
            }
        }

        /// <summary>
        /// 根据日期范围判断表是否存在，如果不存在则创建。
        /// </summary>
        /// <param name="fsql"></param>
        /// <param name="type"></param>
        public static void NonExistsTableBeCreateByColumnRange(this IFreeSql fsql, Type type, object colnumValue1,
            object colnumValue2)
        {
            var tableNames = fsql.CodeFirst.GetTableByEntity(type).AsTableImpl
                .GetTableNamesByColumnValueRange(colnumValue1, colnumValue2);
            foreach (var name in tableNames)
            {
                if (!fsql.DbFirst.ExistsTable(name))
                {
                    fsql.CodeFirst.SyncStructure(type, name);
                }
            }
        }
    }

    public class SharingCoreOptions
    {
        /// <summary>
        /// 配置文件中数据库信息的KEY
        /// </summary>
        public string? DBConfigKey { get; set; } = string.Empty;

        /// <summary>
        /// 空闲过期时间
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// 自定义的数据库信息
        /// </summary>
        public List<DatabaseInfo>? CustomDbConfigs { get; set; } = null;

        /// <summary>
        /// 是否根据SharingCoreDbs中的扩展方法按需加载数据库
        /// </summary>
        public bool DemandLoading { get; set; } = true;

        /// <summary>
        /// 父项目的Assembly，用于扫描SharingCoreDbs扩展方法
        /// </summary>
        public Assembly? BaseReferenceAssembly { get; set; } = null;

        /// <summary>
        /// 对不同库的FreeSqlBuilder进行扩展注入
        /// </summary>
        public Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>? FreeSqlBuildersInject =
            new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>();
    }
}