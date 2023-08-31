using System.Data.Common;

namespace FreeSql.SharingCore.MultiDatabase.Model
{
    public class NoQueryFuncParam
    {
        public IFreeSql Db { get; set; }
        public DbTransaction Transaction { get; set; }
    }
}