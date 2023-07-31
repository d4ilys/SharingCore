using Newtonsoft.Json;
using SharingCore.Assemble.Model;
using SharingCore.Extensions;
using SharingCore.MultiDatabase.Model;
using SharingCore.MultiDatabase.Wrapper;
using TSP.WokerServices.Base;
using WorkerService.Model;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        IFreeSql Logs = Dbs.Logs().GetFreeSql(); //���ֿ�
        IFreeSql Basics; //���ֿ�
        IFreeSql Business_2022; //ͨ���궨λ��
        IFreeSql Business_Now; //ֱ�ӻ�ȡ��ǰ�����ݿ�

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
        }

        public void InitTables()
        {
            Basics = Dbs.Basics().GetFreeSql();
            Business_2022 = Dbs.Business().GetFreeSql("2022");
            Business_Now = Dbs.Business().GetNowFreeSql();
            Basics.CodeFirst.SyncStructure<users>();
            Business_2022.CodeFirst.SyncStructure<order>();
            Business_Now.CodeFirst.SyncStructure<order>();
        }

        public void SharingCoreNoQueryTest()
        {
//ֻ������ǰ��������
            var executeAffrows = Business_Now.Insert(new order
            {
                commodity_name = "iwatch",
                order_time = DateTime.Now,
                buyer_name = "����"
            }).ExecuteAffrows();
            Console.WriteLine(executeAffrows);

//ͨ�����ڷ�Χ���в��� 
            SharingFeatures.NoQuery<order>(noQuery =>
                {
                    noQuery.Db.Insert(new order
                        {
                            commodity_name = "iwatch",
                            order_time = DateTime.Now,
                            buyer_name = "����"
                        })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrows();
                },
                param => param.Init(Dbs.Business(), DateTime.Parse("2023-02-03"),
                    DateTime.Parse("2023-02-03")), //ֻ��д�뵽2023��Ŀ�
                //���񲹳�
                (logId, dbWarp, exception) => { });

            SharingFeatures.NoQuery<order>(noQuery =>
                {
                    noQuery.Db.Insert(new order
                        {
                            commodity_name = "iwatch",
                            order_time = DateTime.Now,
                            buyer_name = "����"
                        })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrows();
                    var next = new Random().Next(2);
                    if (next == 1)
                    {
                        throw new Exception();
                    }
                },
                param => param.Init(Dbs.Business(), DateTime.Parse("2022-02-03"),
                    DateTime.Parse("2023-02-03")), //2022��2023����д��
                // ���񲹳�
                (logId, dbWarp, exception) => { });
        }

        public void QueryPageListTest(int page)
        {
            var result = SharingFeatures.QueryPageList(query =>
                {
                    var result = query.Db.Select<order>()
                        .PageCore(query, out var count)
                        .ToListCore(o => o, query, count);
                    return new QueryFuncResult<order>(result, count);
                },
                param => param.Init(Dbs.Business(), 10, page, DateTime.Parse("2022-12-28"),
                    DateTime.Parse("2023-01-04")),
                out var total);

            Console.WriteLine($"������:{total}����ѯ������{result.Count}");
        }

        public async Task QueryAllTest()
        {
            var list = await SharingFeatures.QueryAsync(query =>
            {
                var list = query.Db.Select<order>()
                    .Where(o => o.order_time.Value.BetweenEnd(query.StartTime, query.EndTime)).ToList();
                return list;
            }, query => query.Init(Dbs.Business(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));
            Console.WriteLine(list.Count);
        }


        public async Task QueryToOneTest()
        {
            var list = await SharingFeatures.QueryToOneAsync(query =>
            {
                var list = query.Db.Select<order>()
                    .Where(o => o.id == 199).ToList();
                return list;
            }, query => query.Init(Dbs.Business(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));
            Console.WriteLine(list.Count);
        }


        public void MultidatabaseTransactionTest(int i)
        {
            var businessWarp = Dbs.Business().GetNowDbWarp();
            var basicsWarp = Dbs.Basics().GetDbWarp();
            using var tran = SharingFeatures.Transaction(businessWarp, basicsWarp);
            tran.OnCommitFail += TransactionCompensation;
            try
            {
                tran.BeginTran();
                var r1 = tran.Orm1.Insert(new order
                {
                    buyer_name = $"����{i}",
                    commodity_name = "����",
                    order_time = DateTime.Now
                }).ExecuteAffrows();

                var r2 = tran.Orm2.Insert<users>(new users()
                {
                    name = $"����{i}",
                    password = "123",
                    username = "1231"
                }).ExecuteAffrows();


                var log = new multi_transaction_log()
                {
                    content = $"{i}�ֲ�ʽ�������...",
                };
                //�ύ���񲢷��ؽ��
                var result = tran.Commit(log);
            }
            catch
            {
                tran.Rellback();
            }

            //�����һ�����ύ�ɹ����������ύ�Ĺ�����ʧ�ܣ���ô������������񲹳�
            void TransactionCompensation(string logId, DbWarp dbWarp, Exception ex)
            {
                //��־���м�¼SQL
                var id = Convert.ToInt64(logId);
                //�����DBWarp����־�洢���ڵ����ݿ�
                var log = dbWarp.Instance.Select<multi_transaction_log>().Where(b => b.id ==
                    id).ToOne();
                //�õ��������ִ�е���Ϣ
                var log_result = JsonConvert.DeserializeObject<List<TransactionsResult>>
                    (log.result_msg);
                foreach (var transactionsResult in log_result)
                {
                    //
                    //�õ�ʧ��Commonʧ�ܵ����ݿ�
                    if (transactionsResult.Successful == false)
                    {
                        //ʧ�����ݿ��KEY
                        var failDb = transactionsResult.Key;
                        //��ȡ���ݿ��������׼��ִ�����񲹳�
                        var tempDb = failDb.GetFreeSql();
                        //�õ��ڷֲ�ʽ�������������Commonʧ�ܵ�SQL
                        var sqls = log.exec_sql;
                        var sqlsDic = JsonConvert.DeserializeObject<Dictionary<string,
                            List<string>>>(sqls);
                        var sqlsList = sqlsDic[failDb];
                        //���񲹳�
                        tempDb.Transaction(() =>
                        {
                            foreach (var noQuerySql in sqlsList)
                            {
                                tempDb.Ado.ExecuteNonQuery(noQuerySql);
                            }

                            if (dbWarp.Instance.Delete<multi_transaction_log>().Where(m => m.id == id)
                                    .ExecuteAffrows() == 0)
                            {
                                throw new Exception("���ɾ����־ʧ�ܣ��ع�SQL..");
                            }
                        });
                        Console.WriteLine("���񲹳��ɹ�...");
                    }
                }
            }
        }

        public void GetFreeSql()
        {
            Logs.Ado.Query<string>("select 1"); //���ֿ�
            Basics.Ado.Query<string>("select 1"); //���ֿ�
            Business_2022.Ado.Query<string>("select 1"); //ͨ���궨λ��
            Business_Now.Ado.Query<string>("select 1"); //ֱ�ӻ�ȡ��ǰ�����ݿ�
        }
    }
}