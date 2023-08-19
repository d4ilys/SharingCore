using FreeSql.Internal.ObjectPool;
using Newtonsoft.Json;
using SharingCore.Assemble.Model;
using SharingCore.MultiDatabase.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using SharingCore.Context;

namespace SharingCore.MultiDatabase.Transcation
{
    public class DistributedTransaction : IDisposable
    {
        //记录需要的数据库的key
        private readonly IEnumerable<DbWarp> _fsqlWarpCollect = new List<DbWarp>();

        //连接池 
        private Dictionary<string, Object<DbConnection>> _connections = new Dictionary<string, Object<DbConnection>>();

        //用到的事务
        internal Dictionary<string, DbTransaction> Transactions = new Dictionary<string, DbTransaction>();

        //日志ID
        private long logId = 0;

        //监听错误提交事件
        internal event Action<string, DbWarp, Exception>? OnCommitFail = null;

        //构造方法
        public DistributedTransaction(IEnumerable<DbWarp> fsqlWarpCollect) => _fsqlWarpCollect = fsqlWarpCollect;

        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTran()
        {
            foreach (var dbInstance in _fsqlWarpCollect)
            {
                var db = dbInstance.Instance;
                //获取连接池对象
                var dbConnection = db.Ado.MasterPool.Get();
                //添加到字典用于归还
                _connections.TryAdd(dbInstance.Name, dbConnection);
                //获取事务对象
                var transaction = dbConnection.Value.BeginTransaction();
                Transactions.TryAdd(dbInstance.Name, transaction);
                CurrentSqlLogContext.ClearSqlLog();
            }
        }

        //提交事务
        //这里的思路：第一个事务必须成功，因为第一个事务需要记录这个事务执行的信息，如果它Common失败，其他事务全部回滚
        public List<TransactionsResult> Commit(multi_transaction_log log)
        {
            var resultDictionary = new List<TransactionsResult>();
            //记录第一个是否已经执行
            var firstRun = false;
            foreach (var kv in Transactions)
            {
                if (!firstRun)
                {
                    try
                    {
                        //获取到第一个
                        var firstTran = kv.Value;
                        log.exec_sql = JsonConvert.SerializeObject(CurrentSqlLogContext.GetSqlLog());
                        var firstIFreeSql = _fsqlWarpCollect.First().Instance;
                        //如果不存日志表，先创建
                        if (!firstIFreeSql.DbFirst.ExistsTable(nameof(multi_transaction_log)))
                        {
                            firstIFreeSql.CodeFirst.SyncStructure<multi_transaction_log>();
                        }

                        logId = firstIFreeSql.Insert(log).WithTransaction(firstTran)
                            .ExecuteIdentity();
                        //第一个Common在提交的时候由于数据库宕机或者是其他原因导致无法提交，全部Rollback
                        firstTran.Commit();
                        resultDictionary.Add(new TransactionsResult()
                        {
                            Key = kv.Key,
                            Successful = true
                        });
                    }
                    catch
                    {
                        resultDictionary.Add(new TransactionsResult()
                        {
                            Key = kv.Key,
                            Successful = false
                        });
                        Rellback();
                        Console.WriteLine("第一个库发生异常，其他全部回滚......");
                        break;
                    }
                    finally
                    {
                        firstRun = true;
                    }
                }
                else
                {
                    // 除了第一个其他事务异常就完蛋了，因为前面的已经提交了，需要记录 告诉上端调用是否成功
                    // 这里从第二个开始提交
                    try
                    {
                        //TODO Test 
                        //if (new Random().Next(2) == 1)
                        //{
                        //    Console.WriteLine($"{kv.Key}，由于网络宕机出现错误 请进行事务补偿.");
                        //    throw new Exception($"{kv.Key}，由于网络宕机出现错误 请进行事务补偿.");
                        //}

                        kv.Value.Commit();

                        //Commit没有异常说明已经执行完成
                        resultDictionary.Add(new TransactionsResult()
                        {
                            Key = kv.Key,
                            Successful = true
                        });
                    }
                    catch (Exception ex)
                    {
                        //运行失败后记录状态
                        resultDictionary.Add(new TransactionsResult()
                        {
                            Key = kv.Key,
                            Successful = false
                        });
                        kv.Value.Rollback();
                        continue;
                    }
                }
            }

            //如果事务全部成功，删除日志
            bool isSuccess = resultDictionary.Any(t => t.Successful == false);
            if (isSuccess == false)
            {
                //删除日志
                _fsqlWarpCollect.First().Instance.Delete<multi_transaction_log>().Where(m => m.id == logId)
                    .ExecuteAffrows();
            }
            else
            {
                var result_msg = JsonConvert.SerializeObject(resultDictionary);
                var upRes = _fsqlWarpCollect.First().Instance.Update<multi_transaction_log>().Set(t => t.successful, 1)
                    .Set(t => t.result_msg, result_msg).Where(t => t.id == logId).ExecuteAffrows();
                if (upRes > 0)
                {
                    //事件注册
                    OnCommitFail?.Invoke(logId.ToString(), _fsqlWarpCollect.First(), null); 
                }
            }
            CurrentSqlLogContext.ClearSqlLog();
            return resultDictionary;
        }

        public void Rellback()
        {
            foreach (var kv in Transactions)
            {
                try
                {
                    kv.Value.Rollback();
                }
                catch
                {
                    continue;
                }
            }

            CurrentSqlLogContext.ClearSqlLog();
        }

        //析构函数
        ~DistributedTransaction() => Dispose();

        public void Dispose()
        {
            try
            {
                foreach (var key in _fsqlWarpCollect)
                {
                    try
                    {
                        var db = key.Instance;
                        //使用完毕归还资源
                        db.Ado.MasterPool.Return(_connections[key.Name]);
                    }
                    catch
                    {
                        continue;
                    }
                }

                CurrentSqlLogContext.ClearSqlLog();
                _connections = null;
                Transactions = null;
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}