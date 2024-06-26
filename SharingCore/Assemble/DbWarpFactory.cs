﻿using FreeSql.SharingCore.Assemble.Model;
using FreeSql.SharingCore.Context;

namespace FreeSql.SharingCore.Assemble
{
    /// <summary>
    /// 数据库包装类工厂方法
    /// </summary>
    public class DbWarpFactory
    {
        /// <summary>
        /// 根据条件获取对应的数据库实例，支持多租户
        /// </summary>
        /// <remarks>如果存在租户：Basics_2024_Tenant01</remarks>
        /// <remarks>如果不存在租户：Basics_2024</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static DbWarp Get(string ident, string separateDbIdent = "", string tenant = "",
            bool disableTenancy = false)
        {
            var key = GetName(ident, separateDbIdent, tenant,disableTenancy);
            var db = GetInstance(key);
            return new DbWarp
            {
                Name = key,
                Instance = db
            };
        }

        /// <summary>
        /// 根据条件获取对应的数据库实例，支持多租户
        /// </summary>
        /// <remarks>如果存在租户：Basics_2024_Tenant01</remarks>
        /// <remarks>如果不存在租户：Basics_2024</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static DbWarp GetByKey(string ident, string tenant)
        {
            if (string.IsNullOrWhiteSpace(ident))
            {
                ident = string.Format(ident, tenant);
            }

            var db = GetInstance(ident);
            return new DbWarp
            {
                Name = ident,
                Instance = db
            };
        }

        /// <summary>
        /// 根据条件获取对应的数据库实例，支持多租户
        /// </summary>
        /// <remarks>如果存在租户：Basics_2024_Tenant01</remarks>
        /// <remarks>如果不存在租户：Basics_2024</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static string GetName(string ident, string separateDbIdent = "", string tenant = "",
            bool disableTenancy = false)
        {
            //这里拿到了数据库配置中的key
            var key = $"{ident}";

            if (!string.IsNullOrWhiteSpace(separateDbIdent))
                key += $"_{separateDbIdent}";

            if (!disableTenancy)
            {
                if (!string.IsNullOrWhiteSpace(tenant))
                {
                    key += $"_{tenant}";
                }
                else if (!string.IsNullOrWhiteSpace(TenantContext.GetTenant()))
                {
                    key += $"_{TenantContext.GetTenant()}";
                }
            }


            return key;
        }

        internal static IFreeSql GetInstance(string key)
        {
            return IdleBusProvider.Instance?.Get(key);
        }
    }
}