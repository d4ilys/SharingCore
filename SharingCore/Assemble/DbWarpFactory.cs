using System;
using SharingCore.Assemble.Model;
using System.Threading;
using SharingCore.Context;

namespace SharingCore.Assemble
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
            var key = GetName(ident, separateDbIdent, tenant);
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
        /// <remarks>如果存在租户：Basics_Tenant01_xxx</remarks>
        /// <remarks>如果不存在租户：Basics_xxx</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static DbWarp GetByKey(string ident,  string tenant)
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
        /// <remarks>如果存在租户：Basics_Tenant01_2022</remarks>
        /// <remarks>如果不存在租户：Basics_2022</remarks>
        /// <param name="ident">数据库标识</param>
        /// <param name="separateDbIdent">分库标识，默认是当前年</param>
        /// <param name="tenant">租户标识，默认没有租户</param>
        /// <returns></returns>
        public static string GetName(string ident, string separateDbIdent = "", string tenant = "")
        {
            //这里拿到了数据库配置中的key
            var key = $"{ident}";

            if (!string.IsNullOrWhiteSpace(separateDbIdent))
                key += $"_{separateDbIdent}";
            if (!string.IsNullOrWhiteSpace(tenant))
            {
                key += $"_{tenant}";
            }
            else if (!string.IsNullOrWhiteSpace(TenantContext.GetTenant()))
            {
                key += $"_{TenantContext.GetTenant()}";
            }

            return key;
        }

        internal static IFreeSql GetInstance(string key)
        {
            return IdleBusProvider.Instance?.Get(key);
        }
    }
}