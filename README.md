
#### 目录

🍧 [**前 言**](#前言)  <br />
✨ [分布式事务](#分布式事务) <br />
☁️ [跨库查询 跨库分页查询](#跨库查询/跨库分页查询) <br />


**前言**

话说2021年开始了一个基于ASP.NET Core On Kubernetes 微服务的项目，谈到微服务 多库环境下 分布式事务、分库分表这些问题都是逃不开的，于是首先从ORM开始调研，由于我身为这个项目的架构师  还是要考虑到一些重要的因素 **功能强大、支持多种数据库（并且行为一致，防止出现换库的情况）、支持分库分表** 等等，这时候第一时间就想到了 [FreeSql](https://github.com/dotnetcore/FreeSql)  ，FreeSql的架构设计非常好，每一种支持的数据库都有对应的Provider实现 做到行为一致，而且支持CodeFirst和DbFirst，分库分表FreeSql也有比较简单切有效的方案，本人也经常向FreeSql的作者叶老板请教学习，非常佩服他的技术与人品，也非常感谢他能做出这么好的ORM框架。

**分布式事务**

既然分库了 分布式事务怎么处理，说到分布式事务 常见的解决方案有TCC/SAGA/消息队列最终一致性，在.NET生态中有基于消息队列实现的分布式事务 [CAP](https://github.com/dotnetcore/CAP) ，TCC和SAGA调研了很久没有发现有比较成熟的实现，那么就决定使用`CAP（最终一致性事务）` 由于项目持续的改版，业务的实时性变得越来越高，基于消息队列的这种最终一致性或者说异步事务的方案 越来越不适合我们的项目，这时候就需要同步的事务方案，TCC/SAGE又没有太好的解决方案（我真的没有找到。。），于是想着自己设计一个，基于FreeSql实现事务管理器。

想要的效果：和单库事务一样，出现错误回滚 但是问题来了 多库呢？不同的数据库呢？

* 在多库事务的开启时，每个库管理开启自己的事务
* 在多库事务提交时，每个库的事务统一提交
* 如果某一个库事务出现异常，这回滚全部数据库事务
* 记录日志，第一个执行Common的数据库称之为主库，会自动创建一个日志表，用于记录多库事务的信息、执行的SQL、业务模块 用于人工介入或者事务补偿
* 如果第一个库发生Common后，其他库可能由于网络原因、数据库宕机 无法Common事务，导致数据不一致，这时候要进行事务补偿或者人工介入

![image](https://user-images.githubusercontent.com/54463101/208339387-8a7cabf4-1afa-43a1-ac62-9f08f89c83fd.png)

**跨库查询/跨库分页查询**

通过时间分片定位、事件委托、分页算法实现跨库分页查询

#### 1.appsettings.json中添加一下配置 

~~~json
  "ShowSqlLog": true,   //是否显示SQL日志
  //数据库配置信息
  "CustomDbConfig": [
    {
      "Key": "sharingcore_basics", //数据库名即可
      "Identification": "sharingcore_basics",
      "DataType": "MySql",
      "ConnectString": "Data Source=host;Port=Port;User ID=root;Password=123;Initial Catalog=sharingcore_basics;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;",
      "Slaves": [
      ]
    },
    //业务库2022、分库
    {
      "Key": "sharingcore_business_2022",
      "Identification": "sharingcore_business", //除去日期的标识
      "DataType": "MySql",
      "ConnectString": "Data Source=host;Port=Port;User ID=root;Password=123;Initial Catalog=sharingcore_business_2022;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;",
      "Slaves": [
      ]
    },
    //业务库2023、分库
    {
      "Key": "sharingcore_business_2023",
      "Identification": "sharingcore_business",
      "DataType": "MySql",
      "ConnectString": "Data Source=host;Port=Port;User ID=root;Password=123;Initial Catalog=sharingcore_business_2022;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;",
      "Slaves": [
      ]
    },
    //日志时序数据库 不分库
    {
      "Key": "sharingcore_log",
      "Identification": "sharingcore_log",
      "DataType": "MySql",
      "ConnectString": "host=host;port=8812;username=admin;password=quest;database=qdb;ServerCompatibilityMode=NoTypeLoading;",
      "Slaves": [
      ]
    }

  ]
~~~

#### 2.初始化数据库

> 创建SharingCoreDbs扩展方法

~~~C#
/// <summary>
/// 基础库
/// </summary>
/// <param name="dbs"></param>
/// <returns></returns>
public static string Basics(this SharingCoreDbs dbs) => "sharingcore_basics";

/// <summary>
/// 主业务库
/// </summary>
/// <param name="dbs"></param>
/// <returns></returns>
[Database(Name = "sharingcore_business_{yyyy}", Separate = "createtime=2022-01-01(1 year)")]
public static string Business(this SharingCoreDbs dbs) => "sharingcore_business";

/// <summary>
/// 日志库
/// </summary>
/// <param name="dbs"></param>
/// <returns></returns>
public static string Logs(this SharingCoreDbs dbs) => "sharingcore_log";
~~~

> 可以创建GlobalUsings.cs更方面

~~~c#
global using static SharingCore.MultiDatabase.Wrapper.SharingCores;
global using SharingCore;
~~~

> ASP.NET Core 6.0/7.0 Program.cs中

~~~c#
var builder = WebApplication.CreateBuilder(args).InjectSharingCore(); //注入
//简单方式
services.AddSharingCore(options =>
{
   options.DBConfigKey = "CustomDbConfig";  //指定配置文件中的KEY，如不指定 默认为 SharingCoreDbConfig
});
//全局过滤器
builder.Services.AddSharingCore<FreeSqlFilter>(f => f.isDelete == 0);
//复杂构建
builder.Services.AddSharingCore(new Dictionary<string, Expression<Func<FreeSqlFilter, bool>>>() //定制不同库的过滤器
{
    { Dbs.Basics(), f => f.isDelete == 0 }, //基础库全局过滤器
    { Dbs.Logs(), f => false }, //此库不指定过滤器
    { FreeSqlFilterType.Communal, f => f.isDelete == 0 } //其他库公用的过滤器
}, options =>
{
    options.DBConfigKey = "CustomDbConfig"; //指定配置文件中的KEY，如不指定 默认为 SharingCoreDbConfig
    options.DemandLoading = true; //按需加载
    //FreeSqlBuilder时候每个库可以扩展
    options.FreeSqlBuildersInject = new Dictionary<string, Func<FreeSqlBuilder, FreeSqlBuilder>>()
    {
        {
            Dbs.Logs(), //日志库QuestDb在FreeSqlBuilder需要制定RestAPI配置
            builder => builder.UseQuestDbRestAPI("192.168.0.36:9001", "admin", "ushahL(aer2r")
        }
    };
});
~~~

* **按需加载：例如 配置文件中有30个数据库但是不同的工作服务中会用到不同的数据库 并不想全都加载，这时候可以每一个服务自定义SharingCoreDbs扩展方法 来控制加载数据库，SharingCore会根据扩展方法进行加载数据库**

> ASP.NET Core 3.1/5.0 Program.cs中

~~~C#
 public static IHostBuilder CreateHostBuilder(string[] args) =>
     Host.CreateDefaultBuilder(args)
     .InjectSharingCore()  //注入
~~~

#### 3.获取IFreeSql操作对象

~~~c#
//不分库
var Logs = Dbs.Logs().GetFreeSql().Ado.Query<string>("select 1"); 
//不分库
var Basics = Dbs.Basics().GetFreeSql().Ado.Query<string>("select 1");
//通过年定位库
var Business_2022 = Dbs.Business().GetFreeSql("2022").Ado.Query<string>("select 1"); 
//直接获取当前年数据库
var Business_2023 = Dbs.Business().GetNowFreeSql().Ado.Query<string>("select 1"); 
~~~

#### 5.跨库分页查询

~~~C#
var result = SharingCores.QueryPageList(query =>
{
    var result = query.Db.Select<order>().PageCore(query, out var count)
     			.ToListCore(o => o, query, count);
                    return new QueryFuncResult<order>(result, count);
},param => param.Init(Dbs.Business(), 10, page, DateTime.Parse("2022-12-28"),DateTime.Parse("2023-01-04")),out var total);
Console.WriteLine($"总条数:{total}，查询条数：{result.Count}");
~~~

#### 6. 跨库增删改

~~~C#
//只会往当前年库里插入
var executeAffrows = Business_Now.Insert(new order
{
    commodity_name = "iwatch",
    order_time = DateTime.Now,
    buyer_name = "张三"
}).ExecuteAffrows();
Console.WriteLine(executeAffrows);

//通过日期范围进行插入 
SharingCores.NoQuery<order>(noQuery =>
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

SharingCores.NoQuery<order>(noQuery =>
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
    (logId, dbWarp, exception) => { })
~~~

#### 7.跨库并行查询（不分页）

~~~C#
var list = await SharingCores.QueryAsync(query =>
{
      var list = query.Db.Select<order>()
      .Where(o => o.order_time.Value.BetweenEnd(query.StartTime, query.EndTime)).ToList();
       return list;
}, query => query.Init(Dbs.Business(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));
Console.WriteLine(list.Count);
~~~

#### 8.跨库ToOne查询

~~~C# 
var list = await SharingCores.QueryToOneAsync(query =>
{
      var list = query.Db.Select<order>()
      .Where(o => o.id == 199).ToList();
      return list;
}, query => query.Init(Dbs.Business(), DateTime.Parse("2022-02-01"), DateTime.Parse("2023-05-01")));
Console.WriteLine(list.Count);
~~~

#### 9.跨库Any查询

~~~C# 
var list = await SharingCore.QueryAnyAsync(func =>
{
    var list = func.Db.Select<vehicleLudan>()
        .Where(l => l.dateLudan.Value.BetweenEnd(func.StartTime, func.EndTime))
        .Where(l => l.id = "1")
        .ToList();
    return list;
}, query => query.Init(DBAll.Business, start, end));
~~~

#### 10.分布式事务、多库事务

~~~C#
var businessWarp = Dbs.Business().GetNowDbWarp();
var basicsWarp = Dbs.Basics().GetDbWarp();
using (var tran = SharingCores.Transaction(businessWarp, basicsWarp))
{
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
        if (new Random().Next(5) == 1)
        {
            throw new Exception("");
        }

        var log = new multi_transaction_log()
        {
            content = $"{i}分布式事务测试...",
        };
        //提交事务并返回结果
        var result = tran.Commit(log);
        Console.WriteLine(result);
    }
    catch
    {
        tran.Rellback();
    }
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
            //事务补偿，执行在Common时没有成功的SQL
            tempDb.Transaction(() => {
                foreach (var noQuerySql in sqlsList)
                {
                    tempDb.Ado.ExecuteNonQuery(noQuerySql);
                }

                if (dbWarp.Instance.Delete<multi_transaction_log>().Where(m => m.id == id).ExecuteAffrows() == 0)
                {
                    throw new Exception("如果删除日志失败，回滚SQL..");
                }
            });
            Console.WriteLine("事务补偿成功...");
        }
    }
}
~~~
