using System;
using System.Collections.Generic;
using System.Text;

namespace SharingCore.Assemble.Enums
{
    public class FreeSqlFilterType
    {
        /// <summary>
        /// 其他没有指定过滤器的库的通用过滤器
        /// </summary>
        public static string Communal => "SharingCore_FreeSqlFilter_Communal";

        ///// <summary>
        ///// 该库无过滤器
        ///// </summary>
        //public static string Non => "SharingCore_FreeSqlFilter_Non";

        ///// <summary>
        ///// 该库的专属过滤器
        ///// </summary>
        //public static string Exclusive => "SharingCore_FreeSqlFilter_Exclusive";
    }
}