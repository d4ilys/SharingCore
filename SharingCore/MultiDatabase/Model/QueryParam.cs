using System;
using System.Collections.Generic;
using System.Text;

namespace SharingCore.MultiDatabase.Model
{
    public class QueryParam
    {
        /// <summary>
        /// 数据库标识
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// 租户标识，可为空
        /// </summary>
        public string Tenant { get; set; } = string.Empty;

        /// <summary>
        /// 日期范围-开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 日期范围-结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 每页数据
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrPage { get; set; }

        /// <summary>
        /// 初始化查询对象
        /// </summary>
        /// <param name="_dbName">数据库Identification</param>
        /// <param name="_pageSize">一页多少数据</param>
        /// <param name="_currPage">当前页</param>
        /// <param name="_startTime">开始日期</param>
        /// <param name="_endTime">结束日期</param>
        public void Init(string _dbName, int _pageSize, int _currPage, DateTime _startTime, DateTime _endTime,
            string _tenant = "")
        {
            DbName = _dbName;
            PageSize = _pageSize;
            CurrPage = _currPage;
            StartTime = _startTime;
            EndTime = _endTime;
            Tenant = _tenant;
        }
    }

    public class QueryNoPageParam
    {
        public string DbName { get; set; }

        /// <summary>
        /// 租户标识，可为空
        /// </summary>
        public string Tenant { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }


        /// <summary>
        /// 初始化查询对象
        /// </summary>
        /// <param name="_dbName">数据库Identification</param>
        /// <param name="_startTime">开始日期</param>
        /// <param name="_endTime">结束日期</param>
        public void Init(string _dbName, DateTime _startTime, DateTime _endTime, string _tenant = "")
        {
            DbName = _dbName;
            StartTime = _startTime;
            EndTime = _endTime;
            Tenant = _tenant;
        }
    }
}