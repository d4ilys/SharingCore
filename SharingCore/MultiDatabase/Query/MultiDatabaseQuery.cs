using FreeSharding.SeparateDatabase;
using Newtonsoft.Json;
using SharingCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharingCore.Assemble;
using SharingCore.MultiDatabase.Model;
using SharingCore.Common;

namespace SharingCore.MultiDatabase.Query
{
    public class MultiDatabaseQuery
    {
        public event Action<Exception>? OnExcetion = null;

        /// <summary>
        /// 跨库分页查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">分页执行的委托</param>
        /// <param name="queryParamAction">入参委托</param>
        /// <param name="count">返回总条数</param>
        /// <returns></returns>
        public List<T> QueryPageList<T>(Func<QueryFuncParam, QueryFuncResult<T>> func,
            Action<QueryParam> queryParamAction, out long count)
        {
            var taskList = new List<T>();
            count = 0;
            try
            {
                //委托初始化参数
                QueryParam queryParam = new QueryParam();
                queryParamAction(queryParam);
                //根据日期范围获取数据库名称
                var separate = SharingCoreUtils.TryGetDateTimeSeparate(queryParam.DbName);
                var dbNamesByTimeRange =
                    separate?.GetDbNamesByColumnValueRange(queryParam.StartTime, queryParam.EndTime).Reverse();
                //记录第一次查询的页数
                long size = 0;
                //偏移量
                int sikp = 0;
                //记录执行次数
                int k = 0;
                var isSikp = false;
                //是不第一个库的最后一个页
                var firstDbLastPage = false;
                if (dbNamesByTimeRange != null)
                {
                    foreach (var nameItem in dbNamesByTimeRange)
                    {
                        var dbKey = string.Empty;
                        try
                        {
                            k++;
                            var dbWarp = DbWarpFactory.GetByKey(nameItem, queryParam.Tenant);
                            if (dbWarp == null)
                            {
                                Console.WriteLine($"{queryParam.DbName},{nameItem}不存在..");
                                continue;
                            }

                            dbKey = dbWarp.Name;
                            var queryFuncResult = new QueryFuncResult<T>();
                            switch (firstDbLastPage)
                            {
                                //第一次查询最后一页了，才进入这
                                case true when size > 0:
                                {
                                    queryParam.CurrPage = queryParam.CurrPage -
                                                          Convert.ToInt32((size + queryParam.PageSize - 1) /
                                                                          queryParam.PageSize);
                                    var param = new QueryFuncParam()
                                    {
                                        CurrPage = queryParam.CurrPage,
                                        Db = dbWarp.Instance,
                                        EndTime = queryParam.EndTime,
                                        StartTime = queryParam.StartTime,
                                        Skip = queryParam.CurrPage == 0 ? 0 : sikp,
                                        HowMany = k,
                                        PageSize = queryParam.PageSize
                                    };
                                    queryFuncResult = func.Invoke(param);
                                    break;
                                }
                                default:
                                {
                                    var param = new QueryFuncParam()
                                    {
                                        CurrPage = queryParam.CurrPage,
                                        Db = dbWarp.Instance,
                                        EndTime = queryParam.EndTime,
                                        StartTime = queryParam.StartTime,
                                        Skip = sikp,
                                        HowMany = k,
                                        PageSize = queryParam.PageSize
                                    };
                                    //这里是第二次执行，上次的查询的条数和分页的条数是一致的，证明下一次查询没必要翻页，或者没必要查询
                                    if (k > 1 && taskList.Count == queryParam.PageSize && taskList.Count != 0)
                                    {
                                        param.CurrPage = 0;
                                        //第二次可以不查询
                                        param.CanNotQuery = true;
                                    }

                                    queryFuncResult = func.Invoke(param);
                                    break;
                                }
                            }

                            var collection = queryFuncResult.Result;
                            size = Convert.ToInt64(queryFuncResult.Count);
                            //计算多次查询的总页数
                            count += size;
                            //判断第一个库是否还能查询到数据或者数据量否足够一页
                            if (collection.Count != 0 || taskList.Count < queryParam.PageSize)
                            {
                                if (size > 0)
                                {
                                    //计算一个库能分多少页，是否是最后一页
                                    var twice = Convert.ToInt32((size + queryParam.PageSize - 1) /
                                                                queryParam.PageSize) <=
                                                queryParam.CurrPage;

                                    switch (k <= 1)
                                    {
                                        //第一个库最后一页剩余多少数据
                                        case true:
                                        {
                                            sikp = (size <= queryParam.PageSize) switch
                                            {
                                                true => Convert.ToInt32(queryParam.PageSize - size),
                                                _ => sikp
                                            };

                                            switch (size > queryParam.PageSize)
                                            {
                                                case true:
                                                    try
                                                    {
                                                        sikp = Convert.ToInt32(queryParam.PageSize -
                                                                               size % queryParam.PageSize);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                    }

                                                    break;
                                            }

                                            //第二个库的分页
                                            if (twice)
                                            {
                                                firstDbLastPage = true;
                                            }

                                            break;
                                        }
                                    }

                                    //第二次查询判断是否合并
                                    if (k > 1)
                                    {
                                        //如果第一次查询条数不够分页条数
                                        if (taskList.Count < queryParam.PageSize && taskList.Count != 0)
                                        {
                                            if (sikp != 0 || taskList.Count != 0)
                                            {
                                                var dts = collection.Take(sikp);
                                                taskList.AddRange(dts);

                                                isSikp = true;
                                            }
                                        }
                                    }

                                    if (!isSikp && taskList.Count != queryParam.PageSize)
                                    {
                                        taskList.AddRange(collection);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{dbKey}，发生异常 异常信息：{e.Message}");
                            OnExcetion?.Invoke(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }

            return taskList;
        }

        /// <summary>
        /// 并行跨库不分页查询-List<T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public async Task<List<T>> QueryAsync<T>(Func<QueryFuncParam, List<T>> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            //通过元组获取多个参数
            var tupleRes = ParallelQueryTaskFactory(func, queryParamAction);
            //获取到线程工厂
            var taskFactory = tupleRes.Item1;
            //获取到线程任务
            var taskList = tupleRes.Item2;
            var list = new List<T>();
            //等待所有线程处理完成
            await taskFactory.ContinueWhenAll(taskList.ToArray(), async tasks =>
            {
                foreach (var task in tasks)
                {
                    list.AddRange(await task);
                }
            });
            return list;
        }

        /// <summary>
        /// 并行跨库ToOne查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public async Task<T> QueryToOneAsync<T>(Func<QueryFuncParam, T> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            //通过元组获取多个参数
            var tupleRes = ParallelQueryTaskFactory(func, queryParamAction);
            //获取到线程工厂
            var taskFactory = tupleRes.Item1;
            //获取到线程任务
            var taskList = tupleRes.Item2;
            var result = default(T);
            //等待所有线程处理完成
            await taskFactory.ContinueWhenAll(taskList.ToArray(), async tasks =>
            {
                foreach (var task in tasks)
                {
                    var res = await task;
                    if (res != null)
                    {
                        result = res;
                    }
                }
            });
            return result;
        }


        /// <summary>
        /// 并行跨库Any查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="queryParamAction"></param>
        /// <returns></returns>
        public async Task<T> QueryAnyAsync<T>(Func<QueryFuncParam, T> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            //通过元组获取多个参数
            var tupleRes = ParallelQueryTaskFactory(func, queryParamAction);
            //获取到线程工厂
            var taskFactory = tupleRes.Item1;
            //获取到线程任务
            var taskList = tupleRes.Item2;
            var result = default(T);
            //等待所有线程处理完成
            await taskFactory.ContinueWhenAll(taskList.ToArray(), async tasks =>
            {
                foreach (var task in tasks)
                {
                    var res = await task;
                    if (Convert.ToBoolean(res))
                    {
                        result = res;
                    }
                }
            });
            return result;
        }

        //并行查询代码复用，为了兼容多种数据结构
        private Tuple<TaskFactory, List<Task<T>>> ParallelQueryTaskFactory<T>(Func<QueryFuncParam, T> func,
            Action<QueryNoPageParam> queryParamAction)
        {
            var queryParam = new QueryNoPageParam();
            queryParamAction(queryParam);

            //根据日期范围获取数据库名称
            var separate = SharingCoreUtils.TryGetDateTimeSeparate(queryParam.DbName);
            var dbNamesByTimeRange =
                separate?.GetDbNamesByColumnValueRange(queryParam.StartTime, queryParam.EndTime);

            TaskFactory taskFactory = new TaskFactory();
            var _taskList = new List<Task<T>>();

            if (dbNamesByTimeRange != null)
            {
                foreach (var nameItem in dbNamesByTimeRange)
                {
                    //多线程并行处理
                    _taskList.Add(taskFactory.StartNew(() =>
                    {
                        T invoke = default(T);
                        try
                        {
                            var ifreesql = DbWarpFactory.GetByKey(nameItem, queryParam.Tenant);
                            var param = new QueryFuncParam()
                            {
                                Db = ifreesql.Instance,
                                EndTime = queryParam.EndTime,
                                StartTime = queryParam.StartTime,
                            };
                            invoke = func.Invoke(param);
                            return invoke;
                        }
                        catch (Exception ex)
                        {
                            OnExcetion?.Invoke(ex);
                        }

                        return invoke;
                    }));
                }
            }


            //通过元组返回多个参数
            var result = new Tuple<TaskFactory, List<Task<T>>>(taskFactory, _taskList);
            return result;
        }
    }
}