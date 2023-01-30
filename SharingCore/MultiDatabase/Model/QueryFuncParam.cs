using System;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore.MultiDatabase.Model
{
    public class QueryFuncParam
    {
        public IFreeSql Db { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int CurrPage { get; set; }

        public int PageSize { get; set; }

        public int Skip { get; set; }

        /// <summary>
        /// 第几次执行
        /// </summary>
        public int HowMany { get; set; }

        /// <summary>
        /// 可以不查询，提升性能
        /// </summary>
        public bool CanNotQuery { get; set; } = false;

        /// <summary>
        /// 可以不查询，提升性能，传入总条数
        /// </summary>
        public bool CanNotQueryFn(long count)
        {
            var flag = false;
            if (CanNotQuery)
            {
                flag = true;
            }
            //全完开始第二库查询后，第一次查询就不用了
            else if (HowMany == 1 && (this.CurrPage * this.PageSize - PageSize) > count)
            {
                flag = true;
            }
            return flag;
        }
    }
}