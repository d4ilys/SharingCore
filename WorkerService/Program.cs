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
                    //���ӹ���
                    services.AddSharingCore(new Dictionary<string, Expression<Func<FreeSqlFilter, bool>>>() //���Ʋ�ͬ��Ĺ�����
                    {
                        { Dbs.Basics(), f => f.isDelete == 0 }, //������ȫ�ֹ�����
                        { Dbs.Logs(), f => false }, //�˿ⲻָ��������
                        { FreeSqlFilterType.Communal, f => f.isDelete == 0 } //�����⹫�õĹ�����
                    }, options =>
                    {
                        options.DBConfigKey = "CustomDbConfig"; //ָ�������ļ��е�KEY���粻ָ�� Ĭ��Ϊ SharingCoreDbConfig
                        options.DemandLoading = true; //�������
                        options.IdleTimeout = TimeSpan.FromSeconds(20);
                        //FreeSqlBuilderʱ��ÿ���������չ
                        options.FreeSqlBuildersInject = new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>()
                        {
                            {
                                Dbs.Logs(), //��־��QuestDb��FreeSqlBuilder��Ҫ�ƶ�RestAPI����
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