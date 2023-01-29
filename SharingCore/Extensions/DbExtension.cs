using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Daily.SharingCore.Assemble;
using Daily.SharingCore.Assemble.Model;
using Daily.SharingCore.Common;
using Daily.SharingCore.MultiDatabase.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.SharingCore.Extensions
{
    public static class DbExtension
    {
        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="initDbConfig">自定义数据库连接对象集合</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore(this IServiceCollection service,
            Func<List<DbConfig>>? initDbConfig = null)
        {
            var configuration = InternalApp.Configuration;
            IdleBusProvider.InitIdleBus(configuration, initDbConfig);
            return service;
        }

        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="filter">全局过滤器，全库统一</param>
        /// <param name="initDbConfig">自定义数据库连接对象集合</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore<T>(this IServiceCollection service,
            Expression<Func<T, bool>> filter, Func<List<DbConfig>>? initDbConfig = null)
        {
            var configuration = InternalApp.Configuration;
            IdleBusProvider.InitIdleBus(configuration, filter, initDbConfig);
            return service;
        }

        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        /// <param name="service"></param>
        /// <param name="filters">全局过滤器，不同库不同过滤器，字典中设置Key为： FreeSqlFilterType.Communal 作用于所有库的过滤器</param>
        /// <param name="initDbConfig">自定义数据库连接对象集合</param>
        /// <remarks>数据库配置：[{"Key":"xxxx","ConnectString":"xxxxx","DataType":"SqlServer","Slaves":["ConnectString","ConnectString","ConnectString"]}]</remarks>
        /// <returns></returns>
        public static IServiceCollection AddSharingCore<T>(this IServiceCollection service,
            Dictionary<string, Expression<Func<T, bool>>> filters, Func<List<DbConfig>>? initDbConfig = null)
        {
            var configuration = InternalApp.Configuration;
            IdleBusProvider.InitIdleBus(configuration, filters, initDbConfig);
            return service;
        }

        /// <summary>
        /// 直接通过数据库标识获取FreeSql对象
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static IFreeSql GetFreeSql(this string dbName, string separateDbIdent = "", string tenant = "")
        {
            return GetDbWarp(dbName, separateDbIdent, tenant).Instance;
        }


        /// <summary>
        /// 直接通过数据库标识获取DbWarp对象，默认是当前年
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
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
}