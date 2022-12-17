using Daily.SharingCore.Assemble.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore.Assemble
{
    /// <summary>
    /// 数据库包装类工厂方法
    /// </summary>
    public class DbWarpFactory
    {
        /// <summary>
        /// 根据条件获取对应的数据库实例，支持多租户
        /// </summary>
        /// <remarks>如果存在租户：Basics_Tenant01_2022</remarks>
        /// <remarks>如果不存在租户：Basics_2022</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static DbWarp Get(string ident, string separateDbIdent = "", string tenant = "")
        {
            //如果不填写 默认是当前年
            //if (string.IsNullOrWhiteSpace(separateDbIdent))
            //{
            //    separateDbIdent = DateTime.Now.Year.ToString();
            //}

            //这里拿到了数据库配置中的key
            var key = $"{ident}";
            if (!string.IsNullOrWhiteSpace(tenant))
                key += $"_{tenant}";
            key += $"_{separateDbIdent}";
            var db = GetInstance(key);
            return new DbWarp
            {
                Name = key,
                Instance = db
            };
        }

        /// <summary>
        /// 根据条件获取对应的数据库实例，不分库
        /// </summary>
        /// <remarks> Basics_Tenant01</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="tenant">租户标识</param>
        /// <returns></returns>
        public static DbWarp Get(string ident, string tenant = "")
        {
            //这里拿到了数据库配置中的key
            var key = ident;
            var db = GetInstance(key);
            return new DbWarp
            {
                Name = key,
                Instance = db
            };
        }

    
        private static IFreeSql GetInstance(string key)
        {
            var db = IdleBusProvider.Instance?.Get(key);
            return db;
        }
    }
}