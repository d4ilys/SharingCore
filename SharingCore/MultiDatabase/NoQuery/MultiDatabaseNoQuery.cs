﻿using FreeSharding.SeparateDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using FreeSql.SharingCore.Assemble.Model;
using FreeSql.SharingCore.Common;
using FreeSql.SharingCore.Extensions;
using FreeSql.SharingCore.MultiDatabase.Model;
using FreeSql.SharingCore.MultiDatabase.Wrapper;
using System.Transactions;

namespace FreeSql.SharingCore.MultiDatabase.NoQuery
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
                    var warp = queryParam.DbName.GetDbWarp(year.Year.ToString(), queryParam.Tenant);
                    dbWarpList.Add(warp);
                }

                //如果需要跨库则走跨库事务
                if (dbWarpList.Count > 1)
                {
                    using (var tran = Wrapper.SharingCore.Transaction(dbWarpList.ToArray())) //集合的第一个记录事务执行日期
                    {
                        //绑定事件
                        if (OnCommitFail != null)
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
                            result = tran.Commit(log).All(r => r.Successful);
                        }
                        catch (Exception e)
                        {
                            tran.Rellback();
                        }
                    }
                }
                else
                {
                    var dbWarp = dbWarpList.FirstOrDefault();
                    var databaseInfo =
                        SharingCoreUtils.DatabaseConfig.DatabaseInfo.FirstOrDefault(info => info.Key == dbWarp.Name);

                    if (!string.Equals(databaseInfo?.DataType, "clickhouse",
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        //获取连接池对象
                        using (var dbConnection = dbWarp?.Instance.Ado.MasterPool.Get())
                        {
                            var transaction = dbConnection.Value.BeginTransaction();
                            try
                            {
                                //执行委托
                                func.Invoke(new NoQueryFuncParam()
                                {
                                    Db = dbWarp.Instance,
                                    Transaction = transaction
                                });

                                transaction.Commit();
                                result = true;
                            }
                            catch (Exception e)
                            {
                                SharingCoreUtils.LogError($"{dbWarp.Name}, NoQuery 单库操作失败, {e.ToString()}");
                                transaction.Rollback();
                            }
                        }
                    }
                    else
                    {
                        func.Invoke(new NoQueryFuncParam()
                        {
                            Db = dbWarp.Instance,
                            Transaction = null
                        });
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


        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <param name="func">分页执行的委托</param>
        /// <param name="queryParamAction">入参委托</param>
        /// <returns></returns>
        public bool NoQuery(Action<NoQueryFuncParam> func,
            Action<NoQueryParam> queryParamAction)
        {
            return NoQuery<string>(func, queryParamAction);
        }
    }
}