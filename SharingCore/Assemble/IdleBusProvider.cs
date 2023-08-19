using FreeSql;
using FreeSql.Aop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using SharingCore.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using SharingCore.Context;

namespace SharingCore.Assemble
{
    /// <summary>
    /// 对象管理容器-IFeeSql实例提供者
    /// </summary>
    public class IdleBusProvider
    {
        private static readonly object LockObject = new object();
        public static IdleBus<IFreeSql>? Instance = null;

        /// <summary>
        /// 初始化时所有FreeSql对象,存放入IdleBus,并声明FreeSql全局过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public static void InitIdleBus(IConfiguration configuration, SharingCoreOptions options)
        {
            void Init()
            {
                //全局过滤器
                InitCommon(configuration, options);
            }

            Init();
            ChangeToken.OnChange(() => configuration?.GetReloadToken(), () =>
            {
                //热更新
                if (configuration != null)
                {
                    Init();
                }
            });
        }


        private static void InitCommon(IConfiguration configuration, SharingCoreOptions options)
        {
            try
            {
                //经典！双if+lock！
                if (Instance == null)
                {
                    lock (LockObject)
                    {
                        Instance ??= new IdleBus<IFreeSql>(options.IdleTimeout);
                        SharingCoreUtils.InitMehtodCache();
                    }
                }

                var dbConfigs = InitConfiguration(configuration, options);

                //连接放入对象管理器
                foreach (var item in dbConfigs.DatabaseInfo)
                {
                    if (Instance.Exists(item.Key)) //删除这个对象然后重新build
                    {
                        try
                        {
                            Instance.TryRemove(item.Key);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    //是否开启了按需加载
                    if (options.DemandLoading)
                    {
                        //如果扩展方法中没有这个数据库则跳出循环
                        if (!SharingCoreUtils.TryIsLoad(item.Key))
                            continue;
                    }

                    Instance.Register(item.Key, () =>
                    {
                        //创建FreeSql对象
                        var freeSqlBuild = new FreeSqlBuilder()
                            .UseConnectionString(DataTypeAdapter.GetDataType(item.DataType), item.ConnectString);
                        //是否显示日志
                        if (dbConfigs.ShowSqlLog)
                        {
                            freeSqlBuild.UseNoneCommandParameter(true).UseMonitorCommand(cmd =>
                            {
                                Console.WriteLine(
                                    "------------------------------------------------------------------------------------------------------------");
                                Console.WriteLine(
                                    $"{item.Key}：监听到SQL-{DateTime.Now:yyy-MM-dd HH:mm:ss}：{cmd.CommandText}{Environment.NewLine}");
                            });
                        }

                        //判断是否配置了读写分离
                        if (item.Slaves.Any())
                        {
                            //配置读写分离
                            freeSqlBuild.UseSlave(item.Slaves.ToArray());
                        }

                        //配置FreeSqlBuilder
                        InjectFreeSqlBuilder(options, ref freeSqlBuild, item.Key);

                        //开始注册
                        var freeSql = freeSqlBuild.Build();

                        //监控日志，拼接字符串
                        freeSql.Aop.CurdAfter += (sender, args) =>
                        {
                            if (args.CurdType != CurdType.Select)
                            {
                                CurrentSqlLogContext.SetSqlLog(item.Key, args.Sql);
                            }
                        };

                        //配置过滤器
                        InjectFreeSqlFilter(options, ref freeSql, item.Key);

                        return freeSql;
                    });
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// 初始化配置
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        /// <param name=""></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static SharingCoreDbConfig InitConfiguration(IConfiguration configuration, SharingCoreOptions? options)
        {
            //获取到Apollo统一配置中心的数据信息
            //初始化数据库对象，支持配置文件和自定义
            var configName = string.IsNullOrWhiteSpace(options?.DBConfigKey)
                ? "SharingCore"
                : options?.DBConfigKey;


            var dbConfigs = configuration.GetSection(configName)?.Get<SharingCoreDbConfig>();

            if (dbConfigs == null)
            {
                var sharingCoreDbConfigString = configuration[configName] ?? "";
                dbConfigs = JsonConvert.DeserializeObject<SharingCoreDbConfig>(sharingCoreDbConfigString);
                if (dbConfigs == null || !dbConfigs.DatabaseInfo.Any())
                {
                    throw new Exception(@"请检查配置文件中配置信息");
                }
            }

            //兼容自定义数据库配置
            if (options?.DatabaseInfoSource != null)
            {
                dbConfigs.DatabaseInfo ??= new List<DatabaseInfo>();
                dbConfigs.DatabaseInfo.AddRange(options?.DatabaseInfoSource);
            }

            return dbConfigs;
        }


        /// <summary>
        ///  对FreeSqlBuilder个性化配置
        /// </summary>
        /// <param name="options"></param>
        /// <param name="freeSqlBuild"></param>
        /// <param name="dbKey"></param>
        private static void InjectFreeSqlBuilder(SharingCoreOptions options, ref FreeSqlBuilder freeSqlBuild,
            string dbKey)
        {
            var flag = true;
            if (options.DatabaseOptions != null)
            {
                var exist = options.DatabaseOptions.TryGetValue(dbKey, out var value);
                if (exist && value != null && value.FreeSqlBuilderInject != null)
                {
                    freeSqlBuild = value.FreeSqlBuilderInject.Invoke(freeSqlBuild);
                    flag = false;
                }
                if (exist && value != null && value.UseAdoConnectionPool)
                {
                    freeSqlBuild.UseAdoConnectionPool(true);
                    flag = false;
                }
            }

            if (flag)
            {
                //FreeSqlBuilder扩展
                if (options.TogetherDatabaseOption != null)
                {
                    if (options.TogetherDatabaseOption.FreeSqlBuilderInject != null)
                    {
                        freeSqlBuild = options.TogetherDatabaseOption.FreeSqlBuilderInject.Invoke(freeSqlBuild);
                    }
                    if (options.TogetherDatabaseOption.UseAdoConnectionPool)
                    {
                        freeSqlBuild.UseAdoConnectionPool(true);
                    }
                }
            }
        }

        /// <summary>
        ///  配置过滤器
        /// </summary>
        /// <param name="options"></param>
        /// <param name="db"></param>
        /// <param name="dbKey"></param>
        private static void InjectFreeSqlFilter(SharingCoreOptions options, ref IFreeSql db,
            string dbKey)
        {
            var flag = true;
            if (options?.DatabaseOptions != null)
            {
                var exist = options.DatabaseOptions.TryGetValue(dbKey, out var value);
                if (exist && value != null && value.FreeSqlFilterExpression != null)
                {
                    db.GlobalFilter.Apply($"{dbKey}_GlobalFilters_{Guid.NewGuid()}", value.FreeSqlFilterExpression);
                    flag = false;
                }
            }

            if (flag)
            {
                //FreeSqlBuilder扩展
                if (options.TogetherDatabaseOption != null)
                {
                    if (options.TogetherDatabaseOption.FreeSqlFilterExpression != null)
                    {
                        db.GlobalFilter.Apply($"{dbKey}_GlobalFilters_{Guid.NewGuid()}",
                            options.TogetherDatabaseOption.FreeSqlFilterExpression);
                    }
                }
            }
        }
    }
}