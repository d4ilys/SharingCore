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

        IFreeSql Logs = Dbs.Logs().GetFreeSql(); //不分库
        IFreeSql Basics; //不分库
        IFreeSql Business_2022; //通过年定位库
        IFreeSql Business_Now; //直接获取当前年数据库

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
//只会往当前年库里插入
            var executeAffrows = Business_Now.Insert(new order
            {
                commodity_name = "iwatch",
                order_time = DateTime.Now,
                buyer_name = "张三"
            }).ExecuteAffrows();
            Console.WriteLine(executeAffrows);

//通过日期范围进行插入 
            SharingFeatures.NoQuery<order>(noQuery =>
                {
                    noQuery.Db.Insert(new order
                        {
                            commodity_name = "iwatch",
                            order_time = DateTime.Now,
                            buyer_name = "张三"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();
                },
                param => param.Init(Dbs.Business(), DateTime.Parse("2023-02-03"),
                    DateTime.Parse("2023-02-03")), //只会写入到2023年的库
                //事务补偿
                (logId, dbWarp, exception) => { });

            SharingFeatures.NoQuery<order>(noQuery =>
                {
                    noQuery.Db.Insert(new order
                        {
                            commodity_name = "iwatch",
                            order_time = DateTime.Now,
                            buyer_name = "张三"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();
                    var next = new Random().Next(2);
                    if (next == 1)
                    {
                        throw new Exception();
                    }
                },
                param => param.Init(Dbs.Business(), DateTime.Parse("2022-02-03"),
                    DateTime.Parse("2023-02-03")), //2022和2023年库均写入
                // 事务补偿
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

            Console.WriteLine($"总条数:{total}，查询条数：{result.Count}");
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
                    buyer_name = $"事务{i}",
                    commodity_name = "事务",
                    order_time = DateTime.Now
                }).ExecuteAffrows();

                var r2 = tran.Orm2.Insert<users>(new users()
                {
                    name = $"事务{i}",
                    password = "123",
                    username = "1231"
                }).ExecuteAffrows();


                var log = new multi_transaction_log()
                {
                    content = $"{i}分布式事务测试...",
                };
                //提交事务并返回结果
                var result = tran.Commit(log);
            }
            catch
            {
                tran.Rellback();
            }

            //如果第一个库提交成功，其他库提交的过程中失败，那么将这里进行事务补偿
            void TransactionCompensation(string logId, DbWarp dbWarp, Exception ex)
            {
                //日志中有记录SQL
                var id = Convert.ToInt64(logId);
                //这里的DBWarp是日志存储所在的数据库
                var log = dbWarp.Instance.Select<multi_transaction_log>().Where(b => b.id ==
                    id).ToOne();
                //拿到多库事务执行的信息
                var log_result = JsonConvert.DeserializeObject<List<TransactionsResult>>
                    (log.result_msg);
                foreach (var transactionsResult in log_result)
                {
                    //
                    //拿到失败Common失败的数据库
                    if (transactionsResult.Successful == false)
                    {
                        //失败数据库的KEY
                        var failDb = transactionsResult.Key;
                        //获取数据库操作对象准备执行事务补偿
                        var tempDb = failDb.GetFreeSql();
                        //拿到在分布式事务中这个库所Common失败的SQL
                        var sqls = log.exec_sql;
                        var sqlsDic = JsonConvert.DeserializeObject<Dictionary<string,
                            List<string>>>(sqls);
                        var sqlsList = sqlsDic[failDb];
                        //事务补偿
                        tempDb.Transaction(() =>
                        {
                            foreach (var noQuerySql in sqlsList)
                            {
                                tempDb.Ado.ExecuteNonQuery(noQuerySql);
                            }

                            if (dbWarp.Instance.Delete<multi_transaction_log>().Where(m => m.id == id)
                                    .ExecuteAffrows() == 0)
                            {
                                throw new Exception("如果删除日志失败，回滚SQL..");
                            }
                        });
                        Console.WriteLine("事务补偿成功...");
                    }
                }
            }
        }

        public void GetFreeSql()
        {
            Logs.Ado.Query<string>("select 1"); //不分库
            Basics.Ado.Query<string>("select 1"); //不分库
            Business_2022.Ado.Query<string>("select 1"); //通过年定位库
            Business_Now.Ado.Query<string>("select 1"); //直接获取当前年数据库
        }
    }
}