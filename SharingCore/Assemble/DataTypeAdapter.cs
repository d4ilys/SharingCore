using System;
using System.Collections.Generic;
using System.Text;
using FreeSql;

namespace SharingCore.Assemble
{
    /// <summary>
    /// 数据库类型适配器
    /// </summary>
    public class DataTypeAdapter
    {
        public static DataType GetDataType(string type)
        {
            DataType dataType = DataType.MySql;
            switch (type.ToLower())
            {
                case "mysql":
                    dataType = DataType.MySql;
                    break;
                case "sqlserver":
                    dataType = DataType.SqlServer;
                    break;
                case "sqlite":
                    dataType = DataType.Sqlite;
                    break;
                case "clickhouse":
                    dataType = DataType.ClickHouse;
                    break;
                case "postgresql":
                    dataType = DataType.PostgreSQL;
                    break; 
                case "msaccess":
                    dataType = DataType.MsAccess;
                    break;
                case "oracle":
                    dataType = DataType.Oracle;
                    break;
                case "firebird":
                    dataType = DataType.Firebird;
                    break;
            }

            return dataType;
        }
    }
}