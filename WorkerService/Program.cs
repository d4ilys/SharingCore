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
                    ////���ӹ���
                    //services.AddSharingCore(new Dictionary<string, Expression<Func<FreeSqlFilter, bool>>>() //���Ʋ�ͬ��Ĺ�����
                    //{
                    //    { Dbs.Basics(), f => f.isDelete == 0 }, //������ȫ�ֹ�����
                    //    { Dbs.Logs(), f => false }, //�˿ⲻָ��������
                    //    { FreeSqlFilterType.Communal, f => f.isDelete == 0 } //�����⹫�õĹ�����
                    //}, options =>
                    //{
                    //    options.DBConfigKey = "CustomDbConfig"; //ָ�������ļ��е�KEY���粻ָ�� Ĭ��Ϊ SharingCore
                    //    options.DemandLoading = true; //�������
                    //    options.IdleTimeout = TimeSpan.FromSeconds(20);
                    //    //FreeSqlBuilderʱ��ÿ���������չ
                    //    options.FreeSqlBuildersInject = new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>()
                    //    {
                    //        {
                    //            Dbs.Logs(), //��־��QuestDb��FreeSqlBuilder��Ҫ�ƶ�RestAPI����
                    //            builder => builder.UseQuestDbRestAPI("192.168.0.36:9001", "admin", "ushahL(aer2r")
                    //        }
                    //    };
                    //});

                    //���ӹ���
                    services.AddSharingCore(options =>
                    {
                        options.DBConfigKey = "CustomDbConfig"; //ָ�������ļ��е�KEY���粻ָ�� Ĭ��Ϊ SharingCore
                        options.DemandLoading = true; //�������
                        options.IdleTimeout = TimeSpan.FromSeconds(20);

                        #region �������ݿ��Զ�������

                        //�������п�Ĺ�����
                        options.TogetherDatabaseOption.FreeSqlFilter<FreeSqlFilter>(f => f.isDelete == 0);
                        //FreeSqlBuilderʱ��ÿ���������չ
                        options.TogetherDatabaseOption.FreeSqlBuilderInject = builder =>
                            builder.UseQuestDbRestAPI("192.168.0.1:9001", "admin", "123");

                        #endregion

                        #region �������ݿ��Զ�������

                        var orderCustomDatabaseSettings = new DatabaseOption();
                        //�������п�Ĺ�����
                        orderCustomDatabaseSettings.FreeSqlFilter<FreeSqlFilter>(f => f.isDelete == 0);
                        //FreeSqlBuilderʱ��ÿ���������չ
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