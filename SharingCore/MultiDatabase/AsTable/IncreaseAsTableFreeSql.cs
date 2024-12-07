using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FreeSql.Internal;

namespace FreeSql.SharingCore.MultiDatabase.AsTable
{
    internal class IncreaseAsTableFreeSql : IFreeSql
    {
        private readonly IFreeSql _orm;

        private readonly string _tableSuffix;

        public IncreaseAsTableFreeSql(IFreeSql orm, string tableSuffix)
        {
            _orm = orm;
            _tableSuffix = tableSuffix;
        }

        public void Dispose() => _orm.Dispose();

        public IInsert<T1> Insert<T1>() where T1 : class => _orm.Insert<T1>().AsTable(s => $"{s}{_tableSuffix}");

        public IInsert<T1> Insert<T1>(T1 source) where T1 : class =>
            _orm.Insert<T1>(source).AsTable(s => $"{s}{_tableSuffix}");

        public IInsert<T1> Insert<T1>(T1[] source) where T1 : class =>
            _orm.Insert<T1>(source).AsTable(s => $"{s}{_tableSuffix}");

        public IInsert<T1> Insert<T1>(List<T1> source) where T1 : class =>
            _orm.Insert<T1>(source).AsTable(s => $"{s}{_tableSuffix}");

        public IInsert<T1> Insert<T1>(IEnumerable<T1> source) where T1 : class =>
            _orm.Insert<T1>(source).AsTable(s => $"{s}{_tableSuffix}");

        public IInsertOrUpdate<T1> InsertOrUpdate<T1>() where T1 : class =>
            _orm.InsertOrUpdate<T1>().AsTable(s => $"{s}{_tableSuffix}");

        public IUpdate<T1> Update<T1>() where T1 : class => _orm.Update<T1>().AsTable(s => $"{s}{_tableSuffix}");

        public IUpdate<T1> Update<T1>(object dywhere) where T1 : class =>
            _orm.Update<T1>(dywhere).AsTable(s => $"{s}{_tableSuffix}");

        public ISelect<T1> Select<T1>() where T1 : class =>
            _orm.Select<T1>().AsTable((type, s) => $"{s}{_tableSuffix}");

        public ISelect<T1> Select<T1>(object dywhere) where T1 : class =>
            _orm.Select<T1>(dywhere).AsTable((type, s) => $"{s}{_tableSuffix}");

        public IDelete<T1> Delete<T1>() where T1 : class => _orm.Delete<T1>().AsTable(s => $"{s}{_tableSuffix}");

        public IDelete<T1> Delete<T1>(object dywhere) where T1 : class =>
            _orm.Delete<T1>(dywhere).AsTable(s => $"{s}{_tableSuffix}");

        public void Transaction(Action handler)
        {
            _orm.Transaction(handler);
        }

        public void Transaction(IsolationLevel isolationLevel, Action handler)
        {
            _orm.Transaction(isolationLevel, handler);
        }

        public IAdo Ado => _orm.Ado;
        public IAop Aop => _orm.Aop;
        public ICodeFirst CodeFirst => _orm.CodeFirst;
        public IDbFirst DbFirst => _orm.DbFirst;
        public GlobalFilter GlobalFilter => _orm.GlobalFilter;
    }
}