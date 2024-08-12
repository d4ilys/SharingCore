using FreeSql.Aop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using FreeSql.SharingCore.Common;
using FreeSql.SharingCore.Context;
using Microsoft.Extensions.Options;

namespace FreeSql.SharingCore.Assemble
{
    /// <summary>
    /// 对象管理容器-IFeeSql实例提供者
    /// </summary>
    internal class IdleBusProvider
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

            if (options.ListeningConfiguration)
            {
                ChangeToken.OnChange(() => configuration?.GetReloadToken(), () =>
                {
                    //热更新
                    if (configuration != null)
                    {
                        Init();
                    }
                });
            }

            //加载完成后执行的动作
            foreach (var action in options.InitializationCompleteExecutor!)
            {
                action.Invoke();
            }
        }


        private static void InitCommon(IConfiguration configuration, SharingCoreOptions options)
        {
            try
            {
                var dbConfigs = InitConfiguration(configuration, options);

                //经典！双if+lock！
                if (Instance == null)
                {
                    lock (LockObject)
                    {
                        Instance ??= new IdleBus<IFreeSql>(options.IdleTimeout);
                        SharingCoreUtils.InitMethodCache();
                    }
                }

                Register(dbConfigs, options);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal static void Register(SharingCoreDbConfig dbConfigs, SharingCoreOptions options)
        {
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

                    //使用默认连接池
                    if (options.UseAdoConnectionPool)
                    {
                        freeSqlBuild.UseAdoConnectionPool(true);
                    }

                    //是否显示日志
                    if (options.ShowSqlLog)
                    {
                        if (dbConfigs.ShowSqlLog)
                        {
                            freeSqlBuild.UseNoneCommandParameter(true).UseMonitorCommand(cmd =>
                            {
                                var logger = SharingCoreUtils.Services.GetService<ILogger<IdleBusProvider>>();
                                logger.LogInformation(
                                    $"{item.Key}：监听到SQL-{DateTime.Now:yyy-MM-dd HH:mm:ss}：{cmd.CommandText}{Environment.NewLine}");
                            });
                        }
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

                    //全局Aop
                    if (options.Aop != null)
                    {
                        options.Aop.Invoke(freeSql);
                    }

                    return freeSql;
                });
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

            SharingCoreUtils.DatabaseConfig = dbConfigs;
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
                if (exist && value != null && value.FreeSqlFilterExpression.Any())
                {
                    foreach (var ex in value.FreeSqlFilterExpression)
                    {
                        db.GlobalFilter.Apply($"{dbKey}_GlobalFilters_{Guid.NewGuid()}", ex);
                    }

                    flag = false;
                }

                if (exist && value != null && value.FreeSqlIfFilterExpression.Any())
                {
                    foreach (var ex in value.FreeSqlIfFilterExpression)
                    {
                        db.GlobalFilter.ApplyIf($"{dbKey}_GlobalFilters_{Guid.NewGuid()}", ex.Condition, ex.Expression,
                            ex.Before);
                    }

                    flag = false;
                }
            }

            if (flag)
            {
                //FreeSqlBuilder扩展
                if (options?.TogetherDatabaseOption != null)
                {
                    if (options.TogetherDatabaseOption.FreeSqlFilterExpression.Any())
                    {
                        foreach (var ex in options.TogetherDatabaseOption.FreeSqlFilterExpression)
                        {
                            db.GlobalFilter.Apply($"{dbKey}_GlobalFilters_{Guid.NewGuid()}", ex);
                        }
                    }

                    if (options.TogetherDatabaseOption.FreeSqlIfFilterExpression.Any())
                    {
                        foreach (var ex in options.TogetherDatabaseOption.FreeSqlIfFilterExpression)
                        {
                            db.GlobalFilter.ApplyIf($"{dbKey}_GlobalFilters_{Guid.NewGuid()}", ex.Condition,
                                ex.Expression, ex.Before);
                        }
                    }
                }
            }
        }
    }
}