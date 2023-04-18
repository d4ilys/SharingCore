using System.Linq.Expressions;
using FreeSql;
using SharingCore.Assemble.Enums;
using SharingCore.Extensions;
using TSP.WokerServices.Base;
using TSP.WokerServices.Base.TSPAdpater;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .InjectSharingCore()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddHostedService<Worker>();
                    //复杂构建
                    services.AddSharingCore(new Dictionary<string, Expression<Func<FreeSqlFilter, bool>>>() //定制不同库的过滤器
                    {
                        { Dbs.Basics(), f => f.isDelete == 0 }, //基础库全局过滤器
                        { Dbs.Logs(), f => false }, //此库不指定过滤器
                        { FreeSqlFilterType.Communal, f => f.isDelete == 0 } //其他库公用的过滤器
                    }, options =>
                    {
                        options.DBConfigKey = "CustomDbConfig"; //指定配置文件中的KEY，如不指定 默认为 SharingCoreDbConfig
                        options.DemandLoading = true; //按需加载
                        options.IdleTimeout = TimeSpan.FromSeconds(20);
                        //FreeSqlBuilder时候每个库可以扩展
                        options.FreeSqlBuildersInject = new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>()
                        {
                            {
                                Dbs.Logs(), //日志库QuestDb在FreeSqlBuilder需要制定RestAPI配置
                                builder => builder.UseQuestDbRestAPI("192.168.0.36:9001", "admin", "ushahL(aer2r")
                            }
                        };
                    });
                })
                .Build();

            host.Run();
        }
    }
}