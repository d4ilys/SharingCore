using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Daily.SharingCore.Assemble.Model;
using Daily.SharingCore.Common;
using Daily.SharingCore.MultiDatabase.Utils;
using FreeSql;
using FreeSql.Aop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Daily.SharingCore.Assemble
{
    /// <summary>
    /// 对象管理容器-IFeeSql实例提供者
    /// </summary>
    public class IdleBusProvider
    {
        private static readonly object LockObject = new object();
        public static IdleBus<IFreeSql>? Instance = null;
        private static readonly List<object> FilterList = new List<object>();

        /// <summary>
        /// 初始化FreeSql对象，存放入IdleBus
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static void InitIdleBus(IConfiguration configuration, Func<IEnumerable<DbConfig>>? initDbConfig = null)
        {
            void Init()
            {
                InitCommon(configuration, null, initDbConfig);
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

        /// <summary>
        /// 初始化时所有FreeSql对象,存放入IdleBus,并声明全局过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="filter"></param>
        public static void InitIdleBus<T>(IConfiguration configuration, Expression<Func<T, bool>> filter,
            Func<IEnumerable<DbConfig>>? initDbConfig = null)
        {
            FilterList.Add(filter);

            void Init()
            {
                //全局过滤器
                InitCommon(configuration, db =>
                {
                    db.GlobalFilter.Apply("GlobalFilters",
                        FilterList.FirstOrDefault() as Expression<Func<T, bool>>);
                    return db;
                }, initDbConfig);
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

        private static void InitCommon(IConfiguration configuration, Func<IFreeSql, IFreeSql>? filterFunc = null,
            Func<IEnumerable<DbConfig>>? initDbConfig = null)
        {
            try
            {
                //经典！双if+lock！
                if (Instance == null)
                {
                    lock (LockObject)
                    {
                        Instance ??= new IdleBus<IFreeSql>(TimeSpan.FromHours(1));
                    }
                }

                //获取到Apollo统一配置中心的数据信息
                //初始化数据库对象，支持配置文件和自定义
                var configName = "SharingCoreDbConfig";


                var dbConfigs = configuration.GetSection(configName)?.Get<List<DbConfig>>();

                if (dbConfigs == null)
                {
                    var sharingCoreDbConfigString = configuration[configName] ?? "";
                    dbConfigs = JsonConvert.DeserializeObject<List<DbConfig>>(sharingCoreDbConfigString);
                    if (dbConfigs == null || !dbConfigs.Any())
                    {
                        throw new Exception(@"请在配置文件中配置数据库连接信息：
 ""SharingCoreDbConfig"": [{
      ""Key"": ""Bussiness"",
      ""Identification"": ""Bussiness"",
      ""DataType"": ""MySql"",
      ""ConnectString"": ""Data Source=Host;Port=Port;User ID=root;Password=xxxxx;Initial Catalog=xxxx;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;"",
      ""Slaves"": []
    },
    //分库分表
    {
      ""Key"": ""Trajectory_2022"",
      ""Identification"": ""Trajectory"",
      ""DataType"": ""MySql"",
      ""ConnectString"": ""Data Source=Host;Port=Port;User ID=root;Password=xxxxx;Initial Catalog=xxxx;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;"",
       ""Slaves"": []
    }]
");
                    }
                }

                if (initDbConfig != null)
                {
                    dbConfigs ??= new List<DbConfig>();

                    dbConfigs.AddRange(initDbConfig.Invoke());
                }

                //连接放入对象管理器
                foreach (var item in dbConfigs)
                {
                    if (Instance.Exists(item.Key)) //删除这个对象然后重新build
                    {
                        try
                        {
                            Instance.TryRemove(item.Key);
                        }
                        catch
                        {
                        }
                    }


                    Instance.Register(item.Key, () =>
                    {
                        //创建FreeSql对象
                        var freeSqlBuild = new FreeSqlBuilder()
                            .UseConnectionString(DataTypeAdapter.GetDataType(item.DataType), item.ConnectString);
                        //是否显示日志
                        if (Convert.ToBoolean(configuration["ShowSqlLog"]))
                        {
                            freeSqlBuild.UseNoneCommandParameter(true).UseMonitorCommand(cmd =>
                            {
                                Console.WriteLine(
                                    "------------------------------------------------------------------------------------------------------------");
                                Console.WriteLine(
                                    $"监听到SQL-{DateTime.Now:yyy-MM-dd HH:mm:ss}：{cmd.CommandText}{Environment.NewLine}");
                            });
                        }

                        //判断是否配置了读写分离
                        if (item.Slaves.Any())
                        {
                            //配置读写分离
                            freeSqlBuild.UseSlave(item.Slaves.ToArray());
                        }

                        //开始注册
                        var freeSql = freeSqlBuild.Build();
                        //全局过滤器
                        if (filterFunc != null && item.IsFilter)
                        {
                            freeSql = filterFunc(freeSql);
                        }

                        //定义过滤器，重写异常
                        //freeSql.Aop.CommandAfter += (sender, args) =>
                        //{
                        //    try
                        //    {
                        //        if (args.Exception != null)
                        //        {
                        //            throw args.Exception;
                        //        }
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        Console.WriteLine($"【{item.Key}】发生异常：{e}");
                        //        throw e;
                        //    }
                        //};

                        //监控日志，拼接字符串
                        freeSql.Aop.CurdAfter += (sender, args) =>
                        {
                            if (args.CurdType != CurdType.Select)
                            {
                                CurdAfterLog.CurrentLog.Value += $"{{\"{item.Key}\":\"{args.Sql}\"}}*t-t*";
                            }
                        };
                        return freeSql;
                    });
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}