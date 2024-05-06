using FreeSql;
using FreeSql.Internal;
using FreeSql.Internal.CommonProvider;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;

namespace FreeSql.SharingCore.MultiDatabase.Transcation
{
    public class DTranFreeSql
    {
        public IFreeSql PureOrm;
        private DbTransaction _transaction;

        public DTranFreeSql(IFreeSql pureOrm, DbTransaction transaction)
        {
            PureOrm = pureOrm;
            _transaction = transaction ?? throw new Exception("指定的事务不存在.");
        }

        public IDelete<T1> Delete<T1>() where T1 : class => PureOrm.Delete<T1>().WithTransaction(_transaction);

        public IDelete<T1> Delete<T1>(object dywhere) where T1 : class => Delete<T1>().WhereDynamic(dywhere);

        public IUpdate<T1> Update<T1>() where T1 : class => PureOrm.Update<T1>().WithTransaction(_transaction);

        public IUpdate<T1> Update<T1>(object dywhere) where T1 : class => Update<T1>().WhereDynamic(dywhere);

        public IInsert<T1> Insert<T1>() where T1 : class => PureOrm.Insert<T1>().WithTransaction(_transaction);

        public IInsert<T1> Insert<T1>(T1 source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(T1[] source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(List<T1> source) where T1 : class => Insert<T1>().AppendData(source);
        public IInsert<T1> Insert<T1>(IEnumerable<T1> source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsertOrUpdate<T1> InsertOrUpdate<T1>() where T1 : class =>
            PureOrm.InsertOrUpdate<T1>().WithTransaction(_transaction);
    }
}