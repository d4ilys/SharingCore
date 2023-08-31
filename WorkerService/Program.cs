using FreeSql;
using FreeSql.SharingCore.Extensions;
using TSP.WokerServices.Base;
using TSP.WokerServices.Base.TSPAdpater;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddHostedService<Worker>();
                    ////复杂构建
                    //services.AddSharingCore(new Dictionary<string, Expression<Func<FreeSqlFilter, bool>>>() //定制不同库的过滤器
                    //{
                    //    { Dbs.Basics(), f => f.isDelete == 0 }, //基础库全局过滤器
                    //    { Dbs.Logs(), f => false }, //此库不指定过滤器
                    //    { FreeSqlFilterType.Communal, f => f.isDelete == 0 } //其他库公用的过滤器
                    //}, options =>
                    //{
                    //    options.DBConfigKey = "CustomDbConfig"; //指定配置文件中的KEY，如不指定 默认为 SharingCore
                    //    options.DemandLoading = true; //按需加载
                    //    options.IdleTimeout = TimeSpan.FromSeconds(20);
                    //    //FreeSqlBuilder时候每个库可以扩展
                    //    options.FreeSqlBuildersInject = new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>()
                    //    {
                    //        {
                    //            Dbs.Logs(), //日志库QuestDb在FreeSqlBuilder需要制定RestAPI配置
                    //            builder => builder.UseQuestDbRestAPI("192.168.0.36:9001", "admin", "ushahL(aer2r")
                    //        }
                    //    };
                    //});

                    //复杂构建
                    services.AddSharingCore(options =>
                    {
                        options.DBConfigKey = "CustomDbConfig"; //指定配置文件中的KEY，如不指定 默认为 SharingCore
                        options.DemandLoading = true; //按需加载
                        options.IdleTimeout = TimeSpan.FromSeconds(20);

                        #region 所有数据库自定义配置

                        //设置所有库的过滤器
                        options.TogetherDatabaseOption.FreeSqlFilter<FreeSqlFilter>(f => f.isDelete == 0);
                        //FreeSqlBuilder时候每个库可以扩展
                        options.TogetherDatabaseOption.FreeSqlBuilderInject = builder =>
                            builder.UseQuestDbRestAPI("192.168.0.1:9001", "admin", "123");

                        #endregion

                        #region 单个数据库自定义配置

                        var orderCustomDatabaseSettings = new DatabaseOption();
                        //设置所有库的过滤器
                        orderCustomDatabaseSettings.FreeSqlFilter<FreeSqlFilter>(f => f.isDelete == 0);
                        //FreeSqlBuilder时候每个库可以扩展
                        orderCustomDatabaseSettings.FreeSqlBuilderInject = builder =>
                            builder.UseNoneCommandParameter(false);
                        options.DatabaseOptions.Add("order", orderCustomDatabaseSettings);

                        #endregion
                    });
                })
                .Build();

            host.Run();
        }
    }
}