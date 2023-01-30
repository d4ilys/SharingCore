using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Daily.SharingCore.MultiDatabase.Model;
using FreeSql;

namespace Daily.SharingCore.Extensions
{
    public static class ISelectExtension
    {
        public static ISelect<T> PageCore<T>(this ISelect<T> select,  QueryFuncParam param, out long count)
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }

        public static ISelect<T1, T2> PageCore<T1, T2>(this ISelect<T1, T2> select,  QueryFuncParam param,
            out long count)
            where T2 : class
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }

        public static ISelect<T1, T2, T3> PageCore<T1, T2, T3>(this ISelect<T1, T2, T3> select, 
            QueryFuncParam param, out long count) where T3 : class where T2 : class
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }

        public static ISelect<T1, T2, T3, T4> PageCore<T1, T2, T3, T4>(this ISelect<T1, T2, T3, T4> select,
             QueryFuncParam param, out long count) where T4 : class where T3 : class where T2 : class
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }

        public static ISelect<T1, T2, T3, T4, T5> PageCore<T1, T2, T3, T4, T5>(
            this ISelect<T1, T2, T3, T4, T5> select,
             QueryFuncParam param, out long count) where T4 : class
            where T3 : class
            where T2 : class
            where T5 : class
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }

        public static ISelect<T1, T2, T3, T4, T5, T6> PageCore<T1, T2, T3, T4, T5, T6>(
            this ISelect<T1, T2, T3, T4, T5, T6> select,
             QueryFuncParam param, out long count) where T4 : class
            where T3 : class
            where T2 : class
            where T5 : class
            where T6 : class
        {
            return select.Limit(param.PageSize).Offset((param.CurrPage - 1) * param.PageSize + param.Skip).Count(out count);
        }
    }

    public static class ISelectToListExtension
    {
        public static List<TReturn> ToListCore<T1, TReturn>(this ISelect<T1> select,
            Expression<Func<T1, TReturn>> param, QueryFuncParam func, long count)
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, TReturn>(this ISelect<T1> select, QueryFuncParam func, long count)
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, TReturn>(this ISelect<T1, T2> select,
            Expression<Func<T1, T2, TReturn>> param, QueryFuncParam func, long count) where T2 : class
        {
            var list = new List<TReturn>();
            if (func.CanNotQueryFn(count) == false) //执行不执行查询，提升性能
                list = select.ToList(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, TReturn>(this ISelect<T1, T2> select, QueryFuncParam func,
            long count) where T2 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, TReturn>(this ISelect<T1, T2, T3> select,
            Expression<Func<T1, T2, T3, TReturn>> param, QueryFuncParam func, long count)
            where T2 : class where T3 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, TReturn>(this ISelect<T1, T2, T3> select,
            QueryFuncParam func, long count)
            where T2 : class where T3 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, TReturn>(this ISelect<T1, T2, T3, T4> select,
            Expression<Func<T1, T2, T3, T4, TReturn>> param, QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, TReturn>(this ISelect<T1, T2, T3, T4> select,
            QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, T5, TReturn>(this ISelect<T1, T2, T3, T4, T5> select,
            Expression<Func<T1, T2, T3, T4, T5, TReturn>> param, QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, T5, TReturn>(this ISelect<T1, T2, T3, T4, T5> select,
            QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, T5, T6, TReturn>(
            this ISelect<T1, T2, T3, T4, T5, T6> select,
            Expression<Func<T1, T2, T3, T4, T5, T6, TReturn>> param, QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count)) //执行不执行查询，提升性能
                list = select.ToList<TReturn>(param);
            return list;
        }

        public static List<TReturn> ToListCore<T1, T2, T3, T4, T5, T6, TReturn>(
            this ISelect<T1, T2, T3, T4, T5, T6> select, QueryFuncParam func, long count)
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            var list = new List<TReturn>();
            if (!func.CanNotQueryFn(count))  //执行不执行查询，提升性能
                list = select.ToList<TReturn>();
            return list;
        }
    }
}