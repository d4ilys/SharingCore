using System;

namespace Daily.SharingCore.MultiDatabase.Model
{
    public class NoQueryParam
    {
        public string DbName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public void Init(string _dbName,DateTime _starTime,DateTime _endTime)
        {
            DbName = _dbName;
            StartTime = _starTime;
            EndTime = _endTime;
        }
    }
}