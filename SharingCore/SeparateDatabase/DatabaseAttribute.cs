using FreeSql.DataAnnotations;
using FreeSql.Internal.Model;
using FreeSql.Internal;
using FreeSql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharingCore.Assemble.Model;

namespace SharingCore
{
    /// <summary>
    /// 分库标识
    /// </summary>
    public class DatabaseAttribute : Attribute
    {
        /// <summary>
        /// 格式：属性名=开始时间(递增)<para></para>
        /// 按年分库：[Database(Name = "log_{yyyy}", AsTable = "create_time=2023-1-1(1 year)")]<para></para>
        /// 按月分库：[Database(Name = "log_{yyyyMM}", AsTable = "create_time=2023-5-1(1 month)")]<para></para>
        /// 按日分库：[Database(Name = "log_{yyyyMMdd}", AsTable = "create_time=2023-5-1(5 day)")]<para></para>
        /// </summary>
        public string? Separate { get; set; }

        public string? Name { get; set; }

        internal DateTimeSeparateImpl ParseSeparate()
        {
            var atm = Regex.Match(Separate,
                @"([\w_\d]+)\s*=\s*(\d\d\d\d)\s*\-\s*(\d\d?)\s*\-\s*(\d\d?)\s*\((\d+)\s*(year|month|day)\)",
                RegexOptions.IgnoreCase);
            if (atm.Success == false)
                throw new Exception("分库标识错误.");

            int.TryParse(atm.Groups[5].Value, out var atm5);
            string atm6 = atm.Groups[6].Value.ToLower();
            return new DateTimeSeparateImpl(Name,
                DateTime.Parse($"{atm.Groups[2].Value}-{atm.Groups[3].Value}-{atm.Groups[4].Value}"), dt =>
                {
                    switch (atm6)
                    {
                        case "year":
                            return dt.AddYears(atm5);
                        case "month":
                            return dt.AddMonths(atm5);
                        case "day":
                            return dt.AddDays(atm5);
                    }

                    throw new NotImplementedException(CoreStrings.Functions_AsTable_NotImplemented(Separate));
                });
        }
    }

    interface ISeparate
    {
        string[] AllDbs { get; }
        string GetDbNameByColumnValue(object columnValue, bool autoExpand = false);
        string[] GetDbNamesByColumnValueRange(object columnValue1, object columnValue2);
    }

    public class DateTimeSeparateImpl : ISeparate
    {
        readonly object _lock = new object();
        readonly List<string> _allDbs = new List<string>();
        readonly List<DateTime> _allDbsTime = new List<DateTime>();
        readonly DateTime _beginTime;
        DateTime _lastTime;
        Func<DateTime, DateTime> _nextTimeFunc;
        string _dbName;
        Match _dbNameFormat;
        static Regex _regTableNameFormat = new Regex(@"\{([^\\}]+)\}");

        public DateTimeSeparateImpl(string dbName, DateTime beginTime, Func<DateTime, DateTime> nextTimeFunc)
        {
            if (nextTimeFunc == null)
                throw new ArgumentException(CoreStrings.Cannot_Be_NULL_Name("nextTimeFunc"));
            beginTime = beginTime.Date; //日期部分作为开始
            _beginTime = beginTime;
            _nextTimeFunc = nextTimeFunc;
            _dbName = dbName;
            _dbNameFormat = _regTableNameFormat.Match(dbName);
            if (string.IsNullOrEmpty(_dbNameFormat.Groups[1].Value))
                throw new ArgumentException(CoreStrings.TableName_Format_Error("yyyyMMdd"));
            ExpandTable(beginTime, DateTime.Now);
        }

        int GetTimestamp(DateTime dt) => (int)dt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        void ExpandTable(DateTime beginTime, DateTime endTime)
        {
            if (beginTime > endTime)
                endTime = _nextTimeFunc(beginTime);
            lock (_lock)
            {
                while (beginTime <= endTime)
                {
                    var dtstr = beginTime.ToString(_dbNameFormat.Groups[1].Value);
                    var name = _dbName.Replace(_dbNameFormat.Groups[0].Value, dtstr);
                    if (_allDbs.Contains(name))
                        throw new ArgumentException(CoreStrings.Generated_Same_SubTable(_dbName));
                    _allDbs.Insert(0, name);
                    _allDbsTime.Insert(0, beginTime);
                    _lastTime = beginTime;
                    beginTime = _nextTimeFunc(beginTime);
                }
            }
        }

        DateTime ParseColumnValue(object columnValue)
        {
            if (columnValue == null)
                throw new Exception(CoreStrings.SubTableFieldValue_IsNotNull);
            DateTime dt;
            if (columnValue is DateTime || columnValue is DateTime?)
                dt = (DateTime)columnValue;
            else if (columnValue is string)
            {
                if (DateTime.TryParse(string.Concat(columnValue), out dt) == false)
                    throw new Exception(CoreStrings.SubTableFieldValue_NotConvertDateTime(columnValue));
            }
            else if (columnValue is int || columnValue is long)
            {
                dt = new DateTime(1970, 1, 1).AddSeconds((double)columnValue);
            }
            else
                throw new Exception(CoreStrings.SubTableFieldValue_NotConvertDateTime(columnValue));

            return dt;
        }

        public string GetDbNameByColumnValue(object columnValue, bool autoExpand = false)
        {
            var dt = ParseColumnValue(columnValue);
            if (dt < _beginTime)
                throw new Exception(CoreStrings.SubTableFieldValue_CannotLessThen(dt.ToString("yyyy-MM-dd HH:mm:ss"),
                    _beginTime.ToString("yyyy-MM-dd HH:mm:ss")));
            var tmpTime = _nextTimeFunc(_lastTime);
            if (dt >= tmpTime && autoExpand)
            {
                // 自动建表
                ExpandTable(tmpTime, dt);
            }

            lock (_lock)
            {
                var allDbsCount = _allDbsTime.Count;
                for (var a = 0; a < allDbsCount; a++)
                    if (dt >= _allDbsTime[a])
                        return _allDbs[a];
            }

            throw new Exception(CoreStrings.SubTableFieldValue_NotMatchTable(dt.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        public string[] GetDbNamesByColumnValueRange(object columnValue1, object columnValue2)
        {
            var dt1 = ParseColumnValue(columnValue1);
            var dt2 = ParseColumnValue(columnValue2);
            if (dt1 > dt2)
                return new string[0];

            lock (_lock)
            {
                int dt1idx = 0, dt2idx = 0;
                var allDbsCount = _allDbsTime.Count;
                if (dt1 < _beginTime)
                    dt1idx = allDbsCount - 1;
                else
                {
                    for (var a = allDbsCount - 2; a > -1; a--)
                    {
                        if (dt1 < _allDbsTime[a])
                        {
                            dt1idx = a + 1;
                            break;
                        }
                    }
                }

                if (dt2 > _allDbsTime.First())
                    dt2idx = 0;
                else
                {
                    for (var a = 0; a < allDbsCount; a++)
                    {
                        if (dt2 >= _allDbsTime[a])
                        {
                            dt2idx = a;
                            break;
                        }
                    }
                }

                if (dt2idx == -1)
                    return new string[0];

                if (dt1idx == allDbsCount - 1 && dt2idx == 0)
                    return _allDbs.ToArray();
                var names = _allDbs.GetRange(dt2idx, dt1idx - dt2idx + 1).ToArray();
                return names;
            }
        }

        public string[] AllDbs
        {
            get
            {
                lock (_lock)
                {
                    return _allDbs.ToArray();
                }
            }
        }
    }
}