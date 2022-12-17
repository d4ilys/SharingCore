using System;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore.MultiDatabase.Model
{
    public class QueryParam
    {
        public string DbName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int PageSize { get; set; }

        public int CurrPage { get; set; }

        /// <summary>
        /// 初始化查询对象
        /// </summary>
        /// <param name="_dbName">数据库Identification</param>
        /// <param name="_pageSize">一页多少数据</param>
        /// <param name="_currPage">当前页</param>
        /// <param name="_startTime">开始日期</param>
        /// <param name="_endTime">结束日期</param>
        public void Init(string _dbName,int _pageSize,int _currPage,DateTime _startTime,DateTime _endTime)
        {
            DbName = _dbName;
            PageSize = _pageSize;
            CurrPage = _currPage;
            StartTime = _startTime;
            EndTime = _endTime;
        }
    }

    public class QueryNoPageParam
    {
        public string DbName
        {
            get; set;
        }

        public DateTime StartTime
        {
            get; set;
        }

        public DateTime EndTime
        {
            get; set;
        }

    

        /// <summary>
        /// 初始化查询对象
        /// </summary>
        /// <param name="_dbName">数据库Identification</param>
        /// <param name="_startTime">开始日期</param>
        /// <param name="_endTime">结束日期</param>
        public void Init(string _dbName, DateTime _startTime, DateTime _endTime)
        {
            DbName = _dbName;
            StartTime = _startTime;
            EndTime = _endTime;
        }
    }
}