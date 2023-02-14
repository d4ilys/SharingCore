using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharingCore.Assemble;
using SharingCore.Assemble.Model;
using SharingCore.Extensions;
using SharingCore.MultiDatabase.Model;
using SharingCore.MultiDatabase.Transcation;
using SharingCore.MultiDatabase.Wrapper;
using FreeSharding.SeparateDatabase;
using FreeSql;
using Newtonsoft.Json;

namespace SharingCore.MultiDatabase.NoQuery
{
    public class MultiDatabaseNoQuery
    {
        //监听错误提交事件
        public Action<string, DbWarp, Exception>? OnCommitFail = null;

        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">分页执行的委托</param>
        /// <param name="queryParamAction">入参委托</param>
        /// <param name="count">返回总条数</param>
        /// <returns></returns>
        public bool NoQuery<T>(Action<NoQueryFuncParam> func,
            Action<NoQueryParam> queryParamAction)
        {
            var result = false;
            try
            {
                var queryParam = new NoQueryParam();
                queryParamAction(queryParam);
                //获取到跨库的信息
                var yearArray = SeparateDatabase.SparateInfo(param =>
                {
                    param.StartTime = queryParam.StartTime;
                    param.EndTime = queryParam.EndTime;
                    param.StrideYear = 1; //几年一次库
                }).YearTimeData;
                //数据库
                yearArray.Reverse();
                var dbWarpList = new List<DbWarp>();
                foreach (var year in yearArray)
                {
                    var warp = queryParam.DbName.GetDbWarp(year.Year.ToString(),queryParam.Tenant);
                    dbWarpList.Add(warp);
                }

                using (var tran = SharingCores.Transaction(dbWarpList.ToArray())) //集合的第一个记录事务执行日期
                {
                    //绑定事件，用于事务补偿
                    tran.OnCommitFail += OnCommitFail;
                    try
                    {
                        //开始事务
                        tran.BeginTran();
                        //向主库添加日志，这个是有事务的
                        var log = new multi_transaction_log()
                        {
                            content = $"跨库执行事务的sql",
                            exec_sql = ""
                        };
                        //多库操作，传递事务
                        foreach (var dbWarp in dbWarpList)
                        {
                            func.Invoke(new NoQueryFuncParam()
                            {
                                Db = dbWarp.Instance,
                                Transaction = tran.Transactions[dbWarp.Name]
                            });
                        }

                        //提交事务并返回结果
                        result = tran.Commit(log).Any();
                    }
                    catch (Exception e)
                    {
                        tran.Rellback();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        }
    }
}