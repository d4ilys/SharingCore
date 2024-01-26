using FreeSql.DataAnnotations;
using FreeSql.SharingCore.Context;
using FreeSql.SharingCore.Extensions;
using FreeSql.SharingCore.MultiDatabase.Model;
using FreeSql.SharingCore.MultiDatabase.Wrapper;
using Newtonsoft.Json;
using TSP.WokerServices.Base;

namespace SeparateDatabaseTable
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TenantContext.SetTenant("lemi");
            Test();
        }

        #region 分库

        public void Test()
        {
            NoQuery(db =>
            {

                var sql = db.Db.Insert(new back_order { reason = "订单取消" }).ToSql();
                Console.WriteLine(sql);
                Console.WriteLine(db.Db.Ado.ConnectionString);
            }, param => param.Init(Dbs.Order(), DateTime.Parse("2023-12-30"),
                DateTime.Parse("2024-08-07")));
        }

        public async Task SeparateDatatableInitAsync()
        {
            //自动创建表
            var list = await SharingCore.QueryAsync(query =>
            {
                query.Db.CodeFirst.SyncStructure<back_order>();
                return new List<string>();
            }, query => query.Init(Dbs.Order(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));

            //通过日期范围进行插入 
            SharingCore.NoQuery<back_order>(noQuery =>
                {
                    noQuery.Db.Insert(new back_order
                        {
                            reason = "2023-08-05订单取消"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();

                    noQuery.Db.Insert(new back_order
                        {
                            reason = "2023-08-05订单取消"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();
                },
                param => param.Init(Dbs.Order(), DateTime.Parse("2023-08-07"),
                    DateTime.Parse("2023-08-07")), //只会写入到2023年的库
                //事务补偿
                (logId, dbWarp, exception) => { });


            //通过日期范围进行插入 
            SharingCore.NoQuery<back_order>(noQuery =>
                {
                    noQuery.Db.Insert(new back_order
                        {
                            reason = "2022-08-07订单取消"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();
                    noQuery.Db.Insert(new back_order
                        {
                            reason = "2022-08-06订单取消"
                        })
                        .WithTransaction(noQuery.Transaction) //可以保证跨库事务
                        .ExecuteAffrows();
                },
                param => param.Init(Dbs.Order(), DateTime.Parse("2022-08-07"),
                    DateTime.Parse("2022-02-07")), //只会写入到2022年的库
                //事务补偿
                (logId, dbWarp, exception) => { });
        }


        public async Task SeparateDatatableAsync()
        {
            //根据时间分库查询
            var list = await SharingCore.QueryAsync(query =>
            {
                var list = query.Db.Select<back_order>().ToList();
                return list;
            }, query => query.Init(Dbs.Order(), DateTime.Parse("2024-01-01"), DateTime.Parse("2024-01-01")));
            Console.WriteLine(JsonConvert.SerializeObject(list));
        }

        public async Task SeparateDatatablePageAsync()
        {
            var result = QueryPageList(query =>
                {
                    var result = query.Db.Select<back_order>().PageCore(query, out var count)
                        .ToListCore(o => o, query, count);
                    return new QueryFuncResult<back_order>(result, count);
                },
                param => param.Init(Dbs.Order(), 10, 1, DateTime.Parse("2022-12-28"), DateTime.Parse("2023-01-04"))
                    .Sort(QueryPageSortType.Ascending), out var total);
            Console.WriteLine($"总条数:{total}，查询条数：{result.Count}");
        }

        #endregion

        #region 分表

        public async Task SeparateTableInitAsync()
        {
            var db = Dbs.Log().GetFreeSql();

            //根据日期创建表
            db.NonExistsTableBeCreateByColumnRange(typeof(logs), DateTime.Parse("2023-08-01"),
                DateTime.Parse("2023-08-07"));
        }

        public void SeparateTableInsert()
        {
            var db = Dbs.Log().GetFreeSql();

            db.Insert(new logs
            {
                content = "2023-08-01日志记录",
                createtime = DateTime.Parse("2023-08-01")
            }).ExecuteAffrows();

            db.Insert(new logs
            {
                content = "2023-08-02日志记录",
                createtime = DateTime.Parse("2023-08-02")
            }).ExecuteAffrows();

            db.Insert(new logs
            {
                content = "2023-08-03日志记录",
                createtime = DateTime.Parse("2023-08-03")
            }).ExecuteAffrows();
        }

        public void SeparateTableUpdate()
        {
            var db = Dbs.Log().GetFreeSql();
            var ct = DateTime.Parse("2023-08-01");
            db.Update<logs>().Set(logs => logs.content, "已经修改").Where(logs => logs.Id == 1 && logs.createtime == ct)
                .ExecuteAffrows();
        }


        public void SeparateTableSelect()
        {
            var db = Dbs.Log().GetFreeSql();
            var start = DateTime.Parse("2023-08-01");
            var end = DateTime.Parse("2023-08-03");
            var logsList = db.Select<logs>().Where(logs => logs.createtime.Between(start, end)).ToList();
            Console.WriteLine(JsonConvert.SerializeObject(logsList));
        }

        #endregion
    }

    public class back_order
    {
        [Column(IsIdentity = true)] public int Id { get; set; }
        public string reason { get; set; }
    }

    //一天分一次表
    [Table(Name = "log_{yyyyMMdd}", AsTable = "createtime=2023-8-1(1 day)")]
    public class logs
    {
        [Column(IsIdentity = true)] public int Id { get; set; }

        public string content { get; set; }

        //通过此字段定位分表
        public DateTime createtime { get; set; }
    }
}