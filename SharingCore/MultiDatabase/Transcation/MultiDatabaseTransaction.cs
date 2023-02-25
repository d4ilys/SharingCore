using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using SharingCore.Assemble.Model;
using SharingCore.MultiDatabase.Model;

namespace SharingCore.MultiDatabase.Transcation
{
    public class MultiDatabaseTransaction2 : IDisposable
    {
        private DistributedTransaction _internel;
        private DbWarp _dbWarp1;
        private DbWarp _dbWarp2;
        public TranFreeSql Orm1;
        public TranFreeSql Orm2;
        public event Action<string, DbWarp, Exception>? OnCommitFail;

        public MultiDatabaseTransaction2(DbWarp dbWarp1, DbWarp dbWarp2)
        {
            _dbWarp1 = dbWarp1;
            _dbWarp2 = dbWarp2;
            _internel = new DistributedTransaction(new DbWarp[] { dbWarp1, dbWarp2 });
            _internel.OnCommitFail += (s, warp, ex) =>
            {
                OnCommitFail?.Invoke(s,warp,ex);
            };
        }

        public void BeginTran()
        {
            _internel.BeginTran();
            Orm1 = new TranFreeSql(_dbWarp1.Instance, _internel.Transactions[_dbWarp1.Name]);
            Orm2 = new TranFreeSql(_dbWarp2.Instance, _internel.Transactions[_dbWarp2.Name]);
        }

        public void Rellback() => _internel.Rellback();

        public List<TransactionsResult> Commit(multi_transaction_log log) => _internel.Commit(log);

        //析构函数
        ~MultiDatabaseTransaction2() => Dispose();

        public void Dispose() => _internel.Dispose();
    }

    public class MultiDatabaseTransaction3 : IDisposable
    {
        private DistributedTransaction local;
        public TranFreeSql Orm1;
        public TranFreeSql Orm2;
        public TranFreeSql Orm3;
        private DbWarp _dbWarp1;
        private DbWarp _dbWarp2;
        private DbWarp _dbWarp3;
        public event Action<string, DbWarp, Exception>? OnCommitFail;

        public MultiDatabaseTransaction3(DbWarp dbWarp1, DbWarp dbWarp2, DbWarp dbWarp3)
        {
            local = new DistributedTransaction(new DbWarp[] { dbWarp1, dbWarp2 });
            _dbWarp1 = dbWarp1;
            _dbWarp2 = dbWarp2;
            _dbWarp3 = dbWarp3;
            local.OnCommitFail += OnCommitFail;
        }


        public void BeginTran()
        {
            local.BeginTran();
            Orm1 = new TranFreeSql(_dbWarp1.Instance, local.Transactions[_dbWarp1.Name]);
            Orm2 = new TranFreeSql(_dbWarp2.Instance, local.Transactions[_dbWarp2.Name]);
            Orm3 = new TranFreeSql(_dbWarp3.Instance, local.Transactions[_dbWarp3.Name]);
        }

        public void Rellback() => local.Rellback();

        public List<TransactionsResult> Commit(multi_transaction_log log) => local.Commit(log);

        //析构函数
        ~MultiDatabaseTransaction3() => Dispose();

        public void Dispose() => local.Dispose();
    }
}