using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore
{
    public class SeparateDatabaseParam
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartTime
        {
            get; set;
        }
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime EndTime
        {
            get; set;
        }
        /// <summary>
        /// 跨几年，默认跨一年
        /// </summary>
        public int StrideYear { get; set; } = 1;
    }
    public class SeparateDatabaseResult
    {
        /// <summary>
        /// 用于判断请求日期是不是合法 0合法，0合法
        /// </summary>
        public int Code
        {
            get; set;
        }
        /// <summary>
        /// 提示的消息
        /// </summary>
        public string? Message
        {
            get; set;
        }

        /// <summary>
        /// 需要操作的数据库+日期
        /// </summary>
        public List<YearList> YearTimeData { get; set; } = new List<YearList>();

    }

    public class YearList
    {
        public int Year { get; set; }
        public SelectTime SelectTime { get; set; }
    }
}
