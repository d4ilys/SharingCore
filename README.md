**注意：此项目目前还是个人试验项目，请勿在生产环境中使用，如果感兴趣请联系我 QQ 963922242**

#### 1.appsettings.json中添加一下配置

~~~json
  "ShowSqlLog": true,   //是否显示SQL日志
  "SharingCoreDbConfig": [			//数据库配置信息
    {
      "Key": "Bussiness_dev",
      "Identification": "Bussiness_dev",
      "DataType": "MySql",
      "ConnectString": "Data Source=Host;Port=Port;User ID=root;Password=xxxxx;Initial Catalog=xxxx;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;",
      "Slaves": [

      ]
    },
    //轨迹表，分库分表
    {
      "Key": "Trajectory_2022",
      "Identification": "Trajectory",
      "DataType": "MySql",
      "ConnectString": "Data Source=Host;Port=Port;User ID=root;Password=xxxxx;Initial Catalog=xxxx;Charset=utf8;SslMode=none;AllowLoadLocalInfile=true;",
       "Slaves": [

      ]
    }
  ]
~~~

#### 2.初始化数据库

> ASP.NET Core 6.0/7.0 Program.cs中

~~~c#
var builder = WebApplication.CreateBuilder(args).InjectSharingCore(); //注入
//不加全局过滤器
builder.Services.AddSharingCore();
//全局过滤器
builder.Services.AddSharingCore<FreeSqlFilter>(f => f.isDelete == 0);
~~~

> ASP.NET Core 3.1/5.0 Program.cs中

~~~C#
 public static IHostBuilder CreateHostBuilder(string[] args) =>
     Host.CreateDefaultBuilder(args)
     .InjectSharingCore()  //注入
~~~

> ASP.NET Core 3.1/5.0 Startup.cs中

~~~c#
//ConfigureServices方法中
//不加全局过滤器
services.AddSharingCore();
//全局过滤器
services.AddSharingCore<FreeSqlFilter>(f => f.isDelete == 0);
~~~

#### 3.创建一个数据库枚举类

~~~C#
//所有数据库，对应的是appsettings.json中的
public class DBAll
{
    public static string Bussiness
    {
        get { return "Bussiness"; }
    }

    public static string Trajectory
    {
        get
        {
            return "Trajectory";
        }
    }
}
~~~

#### 4.获取IFreeSql操作对象

~~~c#
//普通，不分库
var BussinessDb = DBAll.Bussiness.NormalFreeSql(); 
//分库
var TrajectoryDb = DBAll.Trajectory.SharingFreeSql();
~~~

#### 5.跨库分页查询

~~~C#
//直接调用静态方法
var list = SharingCore.QueryPageList(func =>
{
    var result = func.Db.Select<vehicleLudan, vehicleLudanFreight>()
        .Where((l, f) => l.dateLudan.Value.BetweenEnd(func.StartTime, func.EndTime))
        .LeftJoin((l, f) => l.vehicleLudanId == f.vehicleLudanId)
        .PageCore(pageSize, func, out var count) //自定义分页扩展方法
        .ToListCore((l, f) => new dto() //自定义ToList扩展方法
                    {
                        vehicleLudanId = l.vehicleLudanId,
                        departName = f.dayPrice,
                        dateLudan = l.dateLudan
                    }, func, count);
    return new QueryFuncResult<dto>(result, count);
}, query => query.Init(DBAll.Business, pageSize, currPage, start, end), out var total);
~~~

#### 6. 跨库增删改

~~~C#
var list = new List<string>() { "6202334ae20883e02a66e86e", "61cd0407191e4343c2689a60" };
var result = SharingCore.NoQuery<jsd>(func =>
{
    
    func.Db.Update<jsd>()
        .WithTransaction(func.Transaction)  //千万不要忘记WithTransaction....
        .Set(j => j.htId == "修改了")
        .Where(j => list.Contains(j.jsdId))
        .ExecuteAffrows();
}, param => param.Init(DBAll.Business, starTime, endTime), //参数
(s, warp, ex) => //事务补偿
{
	Console.WriteLine("这里要进行事务补偿");
});
return result;
~~~

#### 7.跨库并行查询（不分页）

~~~C#
var list = await SharingCore.QueryAsync(func =>
{
    var list = func.Db.Select<vehicleLudan>()
        .Where(l => l.dateLudan.Value.BetweenEnd(func.StartTime, func.EndTime)).ToList();
    return list;
}, query => query.Init(DBAll.Business, start, end));
~~~

#### 8.跨库ToOne查询

~~~C# 
var list = await SharingCore.QueryToOneAsync(func =>
{
    var list = func.Db.Select<vehicleLudan>()
        .Where(l => l.dateLudan.Value.BetweenEnd(func.StartTime, func.EndTime))
        .Where(l => l.id = "1")
        .ToList();
    return list;
}, query => query.Init(DBAll.Business, start, end));
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

#### 10.分布式事务、跨库事务

~~~C#
public static async Task Test(int param)
{
    var result = new List<TransactionsResult>();
    long logId = 0;
    //开始执行事务
    using (var tran = SharingCore.Transaction(DBAll.Business.NormalDbWarp(), DBAll.Basics.SharingDbWarp())) //集合的第一个记录事务执行日期
    {
        //绑定事件，用于事务补偿
        tran.OnCommitFail += TransactionCompensation;
        try
        {
            //开始事务
            tran.BeginTran();

            //第一个库添加
            var logistic = new logistics()
            {
                logisticsName = param.ToString()
            };
            //千万不要忘了WithTransaction
            DBAll.Business.NormalFreeSql().Instance.Insert(logistic).WithTransaction(tran.Transactions[db1.Name]).ExecuteAffrows();

            //第二个库添加
            var order = new order()
            {
                orderName = param.ToString()
            };
            //千万不要忘了WithTransaction
            DBAll.Basics.NormalFreeSql().Instance.Insert(order).WithTransaction(tran.Transactions[db2.Name]).ExecuteAffrows();

            //向主库添加日志，这个是有事务的
            var log = new multi_transaction_log()
            {
                content = $"分布式事务测试...",
            };
            //提交事务并返回结果
            result = tran.Commit(log);
        }
        catch (Exception e)
        {
	    //如果多个库在执行SQL时出现异常，将全部回滚
            tran.Rellback();
        }
    }
    result.ForEach(t => { Console.WriteLine($"数据库：{t.Key}，执行结果：{t.Successful}"); });
}

//如果第一个库提交成功，其他库提交的过程中失败，那么将这里进行事务补偿
public static void TransactionCompensation(string logId, DbWarp dbWarp, Exception ex)
{
    var id = Convert.ToInt64(logId);
    var log = dbWarp.Instance.Select<multi_transaction_log>().Where(b => b.id == id).ToOne();
    //确认是失败的
    var log_result = JsonConvert.DeserializeObject<List<TransactionsResult>>(log.result_msg);
    foreach (var transactionsResult in log_result)
    {
        if (transactionsResult.Successful == false)
        {
            var failDb = transactionsResult.Key;
            var tempDb = ib.Get(transactionsResult.Key);
            var sqls = log.exec_sql;
            var sqlsDic = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(sqls);
            var sqlsList = sqlsDic[failDb];
            tempDb.Transaction(() =>
            {
                foreach (var noQuerySql in sqlsList)
                {
                    tempDb.Ado.ExecuteNonQuery(noQuerySql);
                }
            });
            dbWarp.Instance.Delete<multi_transaction_log>().Where(m => m.id == id).ExecuteAffrows();
            Console.WriteLine("事务补偿成功...");
        }
    }
}
~~~
