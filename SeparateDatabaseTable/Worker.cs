using FreeSql.DataAnnotations;
using FreeSql.SharingCore.Common;
using FreeSql.SharingCore.Context;
using FreeSql.SharingCore.Extensions;
using FreeSql.SharingCore.MultiDatabase.Model;
using FreeSql.SharingCore.MultiDatabase.Wrapper;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using SeparateDatabaseTable.SharingCoreCommon;
using System.Linq;

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
            await SeparateDatatableInitAsync();
        }

        #region �ֿ�

        public void Test()
        {
            NoQuery(db =>
            {
                var sql = db.Db.Insert(new back_order { reason = "����ȡ��" }).ExecuteAffrows();
            }, param => param.Init(Dbs.Order(), DateTime.Parse("2023-12-30"),
                DateTime.Parse("2024-08-07")));
        }

        public async Task SeparateDatatableInitAsync()
        {
            //�Զ�������
            var list = Handle(db => db.CodeFirst.SyncStructure<back_order>(),
                query => query.Init(Dbs.Order(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));

            //ͨ�����ڷ�Χ���в��� 
            NoQuery(noQuery =>
                {
                    noQuery.Db.Insert(new back_order
                    {
                        reason = "2023-08-05����ȡ��"
                    })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrows();

                    noQuery.Db.Insert(new back_order
                    {
                        reason = "2023-08-05����ȡ��"
                    })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrows();
                },
                param => param.Init(Dbs.Order(), DateTime.Parse("2023-08-07"),
                    DateTime.Parse("2023-08-07")), //ֻ��д�뵽2023��Ŀ�
                                                   //���񲹳�
                (logId, dbWarp, exception) => { });


            //ͨ�����ڷ�Χ���в��� 
            await NoQueryAsync(async noQuery =>
                {
                    await noQuery.Db.Insert(new back_order
                    {
                        reason = "2022-08-07����ȡ��"
                    })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrowsAsync();

                    await noQuery.Db.Insert(new back_order
                    {
                        reason = "2022-08-06����ȡ��"
                    })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrowsAsync();
                },
                param => param.Init(Dbs.Order(), DateTime.Parse("2022-01-07"), DateTime.Parse("2022-02-07")));

            //ͨ�����ڷ�Χ���в��� 
            var r = await NoQueryAsync(async noQuery =>
                {
                    await noQuery.Db.Insert(new back_order
                        {
                            reason = "2022-08-07����ȡ��"
                        })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrowsAsync();
                    await noQuery.Db.Insert(new back_order
                        {
                            reason = "2023-08-06����ȡ��"
                        })
                        .WithTransaction(noQuery.Transaction) //���Ա�֤�������
                        .ExecuteAffrowsAsync();
                },
                param => param.Init(Dbs.Order(), DateTime.Parse("2022-08-07"), DateTime.Parse("2023-02-07")));

            Console.WriteLine(r);
        }


        public async Task SeparateDatatableAsync()
        {
            //����ʱ��ֿ��ѯ
            var list = await SharingCore.QueryAsync(query =>
            {
                var list = query.Db.Select<back_order>().ToList();
                return list;
            }, query => query.Init(Dbs.Order(), DateTime.Parse("2023-01-01"), DateTime.Parse("2023-01-01")));
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
            Console.WriteLine($"������:{total}����ѯ������{result.Count}");
        }

        #endregion

        #region �ֱ�

        public async Task SeparateTableInitAsync()
        {
            var db = Dbs.Log().GetFreeSql();

            //�������ڴ�����
            db.NonExistsTableBeCreateByColumnRange(typeof(logs), DateTime.Parse("2023-08-01"),
                DateTime.Parse("2023-08-07"));
        }

        public void SeparateTableInsert()
        {
            var db = Dbs.Log().GetFreeSql();

            db.Insert(new logs
            {
                content = "2023-08-01��־��¼",
                createtime = DateTime.Parse("2023-08-01")
            }).ExecuteAffrows();

            db.Insert(new logs
            {
                content = "2023-08-02��־��¼",
                createtime = DateTime.Parse("2023-08-02")
            }).ExecuteAffrows();

            db.Insert(new logs
            {
                content = "2023-08-03��־��¼",
                createtime = DateTime.Parse("2023-08-03")
            }).ExecuteAffrows();
        }

        public void SeparateTableUpdate()
        {
            var db = Dbs.Log().GetFreeSql();
            var ct = DateTime.Parse("2023-08-01");
            db.Update<logs>().Set(logs => logs.content, "�Ѿ��޸�").Where(logs => logs.Id == 1 && logs.createtime == ct)
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

        #region FreeSql��̬��չ

        public static void Init()
        {
            var fsql = Dbs.Common().GetFreeSqlByKey();
            //fsql.CodeFirst.SyncStructure<TenantDbConfig>();
            //fsql.CodeFirst.SyncStructure<TenantSeparateRule>();

            //fsql.Insert(new List<TenantDbConfig>()
            //{
            //    new()
            //    {
            //        Key = "order_2022_lemi",
            //        Identification = "order",
            //        ConnectString = "data source=db/order_2022_lemi.db;",
            //        DataType = "sqlite",
            //    },
            //    new()
            //    {
            //        Key = "order_2023_lemi",
            //        Identification = "order",
            //        ConnectString = "data source=db/order_2023_lemi.db;",
            //        DataType = "sqlite",
            //    },
            //    new()
            //    {
            //        Key = "order_2024_lemi",
            //        Identification = "order",
            //        ConnectString = "data source=db/order_2024_lemi.db;",
            //        DataType = "sqlite",
            //    },
            //}).ExecuteAffrows();

            //fsql.Insert(new TenantSeparateRule
            //{
            //    Name = "order",
            //    Template = "order_{yyyy}",
            //    Separate = "createtime=2022-01-01(1 year)"
            //}).ExecuteAffrows();
        }

        public static void TestAppend()
        {
            var fsql = Dbs.Common().GetFreeSqlByKey();
            var tenantDbConfigs = fsql.Select<TenantDbConfig>().ToList();
            var tenantSeparateRule = fsql.Select<TenantSeparateRule>().ToList();

            Console.WriteLine(string.Join("-", SharingCoreUtils.IdleBusRegisterInfo()));

            SharingCoreUtils.IdleBusAppend(new SharingCoreDbConfig()
            {
                DatabaseInfo = tenantDbConfigs.Select(config => new DatabaseInfo
                {
                    Key = config.Key,
                    Identification = config.Identification,
                    ConnectString = config.ConnectString,
                    DataType = config.DataType
                }).ToList(),
                SeparateRules = tenantSeparateRule.Select(rule => new SeparateRule
                {
                    Name = rule.Name,
                    Template = rule.Template,
                    Separate = rule.Separate,
                }).ToList()
            });

            Console.WriteLine(string.Join("-", SharingCoreUtils.IdleBusRegisterInfo()));

            Console.WriteLine(JsonConvert.SerializeObject(SharingCoreUtils.DatabaseConfig.DatabaseInfo));
            Console.WriteLine(JsonConvert.SerializeObject(SharingCoreUtils.DatabaseConfig.SeparateRules));
            Console.WriteLine(JsonConvert.SerializeObject(SharingCoreUtils.Options));
        }

        #endregion
    }

    public class back_order
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        public string reason { get; set; }
    }

    //һ���һ�α�
    [Table(Name = "log_{yyyyMMdd}", AsTable = "createtime=2023-8-1(1 day)")]
    public class logs
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        public string content { get; set; }

        //ͨ�����ֶζ�λ�ֱ�
        public DateTime createtime { get; set; }
    }

    public class TenantDbConfig
    {
        /// <summary>
        /// Key��������Ψһ�ı�ʶ��Key��ʽ{���ݿ�ǰ׺(Identification)}_�⻧��ʶ_�ֿ��ʶ
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// ���ݿ�����ǰ׺������ֿ⣺Business_tenant01_2022�������IdentificationeӦ����Business
        /// </summary>
        public string? Identification { get; set; }

        /// <summary>
        /// ���ݿ������ַ���
        /// </summary>
        public string? ConnectString { get; set; }

        /// <summary>
        /// ���ݿ�����
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// ��д����-�ӿ�
        /// </summary>
        public List<string> Slaves { get; set; } = new List<string>();
    }

    public class TenantSeparateRule
    {
        public string Name { get; set; }

        public string Template { get; set; }

        /// <summary>
        /// �ֿ����
        /// </summary>
        public string? Separate { get; set; }
    }
}