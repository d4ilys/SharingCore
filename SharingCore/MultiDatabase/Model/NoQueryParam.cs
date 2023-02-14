using System;

namespace SharingCore.MultiDatabase.Model
{
    public class NoQueryParam
    {
        public string DbName { get; set; }

        /// <summary>
        /// 租户标识，可为空
        /// </summary>
        public string Tenant { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public void Init(string _dbName,DateTime _starTime,DateTime _endTime,string _tenant)
        {
            DbName = _dbName;
            StartTime = _starTime;
            EndTime = _endTime;
            Tenant = _tenant;
        }
    }
}