﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeSql.SharingCore.Assemble.Model;
using FreeSql.SharingCore.MultiDatabase.Model;
using FreeSql.SharingCore.MultiDatabase.NoQuery;
using FreeSql.SharingCore.MultiDatabase.Query;
using FreeSql.SharingCore.MultiDatabase.Transcation;

namespace FreeSql.SharingCore.MultiDatabase.Wrapper
{
    /// <summary>
    /// 包装类
    /// </summary>
    public class SharingCore
    {
        /// <summary>
        /// 多库事务
        /// </summary>
        /// <param name="param">Db包装类</param>
        /// <returns></returns>
        public static DistributedTransaction Transaction(IEnumerable<DbWarp> dbWarps)
        {
            var local = new AsyncLocal<DistributedTransaction>
            {
                Value = new DistributedTransaction(dbWarps)
            };

            return local.Value;
        }

        /// <summary>
        /// 多库事务
        /// </summary>
        /// <param name="dbWarps"></param>
        /// <returns></returns>
        public static MultiDatabaseTransaction TransactionMany(IEnumerable<DbWarp> dbWarps)
        {
            var local = new AsyncLocal<MultiDatabaseTransaction>
            {
                Value = new MultiDatabaseTransaction(dbWarps)
            };

            return local.Value;
        }

        /// <summary>
        /// 多库事务
        /// </summar y>
        /// <param name="param">Db包装类</param>
        /// <returns></returns>
        public static MultiDatabaseTransaction2 Transaction(DbWarp dbWarp1, DbWarp dbWarp2)
        {
            var local = new AsyncLocal<MultiDatabaseTransaction2>();
            local.Value = new MultiDatabaseTransaction2(dbWarp1, dbWarp2);
            return local.Value;
        }

        /// <summary>
        /// 多库事务
        /// </summary>
        /// <param name="param">Db包装类</param>
        /// <returns></returns>
        public static MultiDatabaseTransaction3 Transaction(DbWarp dbWarp1, DbWarp dbWarp2, DbWarp dbWarp3)
        {
            var local = new AsyncLocal<MultiDatabaseTransaction3>();
            local.Value = new MultiDatabaseTransaction3(dbWarp1, dbWarp2, dbWarp3);
            return local.Value;
        }

        /// <summary>
        /// 跨库分页查询，包装方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// 
        public static List<T> QueryPageList<T>(Func<QueryFuncParam, QueryFuncResult<T>> func,
            Action<QueryParam> queryParamAction, out long count)
        {
            var multiDatabase = new MultiDatabaseQuery();
            Exception? ex = null;
            multiDatabase.OnExcetion += res => { ex = res; };
            var result = multiDatabase.QueryPageList<T>(func, queryParamAction, out count);
            //发生异常直接抛出
            if (ex != null)
            {
                throw ex;
            }

            return result;
        }

        /// <summary>
        /// 并行跨库不分页查询-List<T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public static async Task<List<T>> QueryAsync<T>(Func<QueryFuncParam, List<T>> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            var multiDatabase = new MultiDatabaseQuery();
            Exception? ex = null;
            multiDatabase.OnExcetion += res => { ex = res; };
            var result = await multiDatabase.QueryAsync(func, queryParamAction);
            //发生异常直接抛出
            if (ex != null)
            {
                throw ex;
            }

            return result;
        }

        /// <summary>
        /// 并行跨库ToOne查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public static async Task<T> QueryToOneAsync<T>(Func<QueryFuncParam, T> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            var multiDatabase = new MultiDatabaseQuery();
            Exception? ex = null;
            multiDatabase.OnExcetion += res => { ex = res; };
            var result = await multiDatabase.QueryToOneAsync(func, queryParamAction);
            //发生异常直接抛出
            if (ex != null)
            {
                throw ex;
            }

            return result;
        }

        /// <summary>
        /// 并行跨库Any查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public static async Task<T> QueryAnyAsync<T>(Func<QueryFuncParam, T> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            var multiDatabase = new MultiDatabaseQuery();
            Exception? ex = null;
            multiDatabase.OnExcetion += res => { ex = res; };
            var result = await multiDatabase.QueryAnyAsync(func, queryParamAction);
            //发生异常直接抛出
            if (ex != null)
            {
                throw ex;
            }

            return result;
        }

        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">执行的委托</param>
        /// <param name="condition">入参委托</param>
        /// <param name="compensation">事务补偿委托</param>
        /// <returns></returns>
        public static bool NoQuery<T>(Action<NoQueryFuncParam> func,
            Action<NoQueryParam> condition, Action<string, DbWarp, Exception>? compensation = null)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            if (compensation == null)
            {
                multiDatabaseNoQuery.OnCommitFail = compensation;
            }

            var result = multiDatabaseNoQuery.NoQueryAsync<T>(param =>
            {
                func(param);
                return Task.CompletedTask;
            }, condition).GetAwaiter().GetResult();

            return result;
        }

        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">执行的委托</param>
        /// <param name="condition">入参委托</param>
        /// <param name="compensation">事务补偿委托</param>
        /// <returns></returns>
        public static async Task<bool> NoQueryAsync<T>(Func<NoQueryFuncParam, Task> func,
            Action<NoQueryParam> condition, Action<string, DbWarp, Exception>? compensation = null)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            if (compensation == null)
            {
                multiDatabaseNoQuery.OnCommitFail = compensation;
            }

            var result = await multiDatabaseNoQuery.NoQueryAsync<T>(func, condition);
            return result;
        }

        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">执行的委托</param>
        /// <param name="condition">入参委托</param>
        /// <param name="compensation">事务补偿委托</param>
        /// <returns></returns>
        public static bool NoQuery(Action<NoQueryFuncParam> func,
            Action<NoQueryParam> condition, Action<string, DbWarp, Exception>? compensation = null)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            if (compensation == null)
            {
                multiDatabaseNoQuery.OnCommitFail = compensation;
            }

            var result = multiDatabaseNoQuery.NoQueryAsync(p =>
            {
                func(p);
                return Task.CompletedTask;
            }, condition).ConfigureAwait(false).GetAwaiter().GetResult();

            return result;
        }

        /// <summary>
        /// 增删改跨库操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">执行的委托</param>
        /// <param name="condition">入参委托</param>
        /// <param name="compensation">事务补偿委托</param>
        /// <returns></returns>
        public static async Task<bool> NoQueryAsync(Func<NoQueryFuncParam, Task> func,
            Action<NoQueryParam> condition, Action<string, DbWarp, Exception>? compensation = null)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            if (compensation == null)
            {
                multiDatabaseNoQuery.OnCommitFail = compensation;
            }

            var result = await multiDatabaseNoQuery.NoQueryAsync(func, condition);

            return result;
        }

        /// <summary>
        /// 跨库操作
        /// </summary>
        /// <param name="dbAction">跨年数据库</param>
        /// <param name="condition">注意 委托内异常需要处理</param>
        /// <returns></returns>
        public static bool Handle(Action<IFreeSql> dbAction,
            Action<NoQueryParam> condition)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            var result = multiDatabaseNoQuery.HandleAsync(p =>
            {
                dbAction(p);
                return Task.CompletedTask;
            }, condition);
            return result.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 跨库操作
        /// </summary>
        /// <param name="dbAction">跨年数据库</param>
        /// <param name="condition">注意 委托内异常需要处理</param>
        /// <returns></returns>
        public static async Task<bool> HandleAsync(Func<IFreeSql, Task> dbAction,
            Action<NoQueryParam> condition)
        {
            var multiDatabaseNoQuery = new MultiDatabaseNoQuery();
            var result = await multiDatabaseNoQuery.HandleAsync(dbAction, condition);
            return result;
        }

        /// <summary>
        /// 数据库枚举
        /// </summary>
        public static SharingCoreDbs Dbs
        {
            get => new SharingCoreDbs();
            private set { }
        }
    }
}