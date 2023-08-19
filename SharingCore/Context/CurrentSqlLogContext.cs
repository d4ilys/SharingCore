using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharingCore.Context
{
    internal class CurrentSqlLogContext
    {
        static AsyncLocal<List<SqlLogJson>> _logs = new AsyncLocal<List<SqlLogJson>>();

        public static void SetSqlLog(string key, string value)
        {
            _logs.Value ??= new List<SqlLogJson>();

            var any = _logs.Value.Any(s => s.Key == key);

            //如果存在则添加
            if (any)
            {
                _logs.Value.First(s => s.Key == key).Sqls.Add(value);
            }
            else
            {
                var sqlLogJson = new SqlLogJson()
                {
                    Key = key,
                };
                sqlLogJson.Sqls.Add(value);
                _logs.Value.Add(sqlLogJson);
            }
        }

        public static void ClearSqlLog()
        {
            _logs.Value = null;
        }


        public static List<SqlLogJson> GetSqlLog()
        {
            return _logs.Value;
        }
    }

    internal class SqlLogJson
    {
        public string Key { get; set; }

        public List<string> Sqls { get; set; } = new List<string>();
    }
}