using FreeSql.SharingCore.Assemble;
using FreeSql.SharingCore.Assemble.Model;
using FreeSql.SharingCore.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace FreeSql.SharingCore.Extensions
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
            Action<SharingCoreOptions>? options = null)
        {
            var option = new SharingCoreOptions();
            options?.Invoke(option);
            SharingCoreUtils.Options = option;
            service.AddHttpContextAccessor();
            service.AddHostedService<GenericHostedService>();
            return service;
        }


        /// <summary>
        /// 直接通过数据库标识获取FreeSql对象
        /// </summary>
        /// <param name="dbName">数据库标识，对应配置文件中的Identification</param>
        /// <param name="separateDbIdent">分库标识，默认没有分库标识</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <param name="disableTenancy">禁用多租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static IFreeSql GetFreeSql(this string dbName, string separateDbIdent = "", string tenant = "",
            bool disableTenancy = false)
        {
            return GetDbWarp(dbName, separateDbIdent, tenant, disableTenancy).Instance;
        }

        /// <summary>
        /// 直接通过数据库标识直接FreeSql对象
        /// </summary>
        /// <param name="dbName">数据库标识Key</param>
        /// <returns></returns>
        public static IFreeSql GetFreeSqlByKey(this string key)
        {
            return IdleBusProvider.Instance.Get(key);
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
        /// <param name="disableTenancy">禁用多租户</param>
        /// <remarks>提示：数据库标识(对应配置文件中的Identification) + 租户标识(租户标识，默认没有租户) + 分库标识(分库标识，默认是当前年) = 数据库名称(对应配置文件中的Key)</remarks>
        /// <returns></returns>
        public static DbWarp GetDbWarp(this string dbName, string separateDbIdent = "", string tenant = "",
            bool disableTenancy = false)
        {
            //TODO disableTenancy
            return string.IsNullOrWhiteSpace(separateDbIdent)
                ? DbWarpFactory.Get(ident: dbName, tenant: tenant, disableTenancy: disableTenancy)
                : DbWarpFactory.Get(dbName, separateDbIdent, tenant, disableTenancy);
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
            var name = SharingCoreUtils.GetDbNamesByColumnValueRange(dbName, tenant, DateTime.Now).FirstOrDefault();
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
}