using System.Data.Common;

namespace SharingCore.MultiDatabase.Model
{
    public class NoQueryFuncParam
    {
        public IFreeSql Db { get; set; }
        public DbTransaction Transaction { get; set; }
    }
}