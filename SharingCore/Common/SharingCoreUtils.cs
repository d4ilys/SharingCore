using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FreeSql.SharingCore.Assemble;
using FreeSql.SharingCore.Context;
using FreeSql.SharingCore.MultiDatabase.Wrapper;

namespace FreeSql.SharingCore.Common
{
    /// <summary>
    /// SharingCore工具类
    /// </summary>
    public partial class SharingCoreUtils
    {
        private static HashSet<DbInfoByAttribute> dbInfosCache = new HashSet<DbInfoByAttribute>();

        internal static void InitMethodCache()
        {
            var type = typeof(SharingCoreDbs);
            var entryAssembly = Assembly.GetEntryAssembly();
            var assmblys = new List<Assembly>() { entryAssembly };
            var baseAssmbly = Options?.BaseReferenceAssembly;
            if (baseAssmbly != null)
            {
                assmblys.Add(baseAssmbly);
            }

            GetExtensionMethods(assmblys, type);
        }

        /// <summary>
        /// 通过SharingCoreDbs中的扩展方法 判断是否加载数据库
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool TryIsLoad(string name)
        {
            try
            {
                return dbInfosCache.Any(m => name.ToLower().StartsWith(m.Name.ToLower()));
            }
            catch
            {
                return true;
            }
        }

        internal static DateTimeSeparateImpl? TryGetDateTimeSeparate(string name)
        {
            try
            {
                return dbInfosCache.FirstOrDefault(m => m.Name.ToLower() == name.ToLower())?.DateTimeSeparate;
            }
            catch
            {
                return null;
            }
        }

        internal static IEnumerable<string> GetDbNamesByColumnValueRange(string name, string tenant,
            object columnValue1, object? columnValue2 = null)
        {
            List<string> dbList = new List<string>();
            if (columnValue2 != null)
            {
                dbList.AddRange(TryGetDateTimeSeparate(name).GetDbNamesByColumnValueRange(columnValue1, columnValue2));
            }
            else
            {
                dbList.Add(TryGetDateTimeSeparate(name).GetDbNameByColumnValue(columnValue1));
            }

            if (!string.IsNullOrWhiteSpace(tenant))
            {
                for (var i = 0; i < dbList.Count; i++)
                {
                    var db = dbList[i];
                    dbList[i] = $"{db}_{tenant}";
                }
            }
            else
            {
                var overallTenantValue = TenantContext.GetTenant();
                if (!string.IsNullOrWhiteSpace(overallTenantValue))
                {
                    for (var i = 0; i < dbList.Count; i++)
                    {
                        var db = dbList[i];
                        dbList[i] = $"{db}_{overallTenantValue}";
                    }
                }
            }

            return dbList;
        }

        static void GetExtensionMethods(IEnumerable<Assembly> assemblys, Type extendedType)
        {
            foreach (var assembly in assemblys)
            {
                var query = assembly.GetTypes()
                    .Where(type => !type.IsGenericType && !type.IsNested)
                    .SelectMany(
                        type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                        (type, method) => new { type, method })
                    .Where(@t => @t.method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    .Where(@t => @t.method.GetParameters()[0].ParameterType == extendedType)
                    .Select(@t => @t);
                foreach (var item in query)
                {
                    var attribute = item.method.GetCustomAttribute<DatabaseAttribute>();

                    var dbInfoByAttribute = new DbInfoByAttribute();

                    //得到扩展方法的返回值
                    var res = item.method.Invoke(null, new object?[]
                    {
                        Activator.CreateInstance(extendedType)
                    });
                    dbInfoByAttribute.Name = res.ToString();

                    //如果没有标记特性默认是扩展方法的名称
                    if (attribute != null)
                    {
                        //如果分库
                        if (!string.IsNullOrWhiteSpace(attribute.Separate))
                        {
                            dbInfoByAttribute.DateTimeSeparate = attribute.ParseSeparate();
                        }
                    }

                    dbInfosCache.Add(dbInfoByAttribute);
                }
            }
        }

        /// <summary>
        /// 反射获取静态方法
        /// </summary>
        /// <param name="extendedType"></param>
        /// <returns></returns>
        static IEnumerable<string> GetExtensionMethods(Type extendedType)
        {
            var methodInfos =
                extendedType.GetMethods();
            var methodNames = methodInfos
                .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                .Where(m => m.GetParameters()[0].ParameterType == extendedType)
                .Select(m => m.Name);
            return methodNames;
        }

        /// <summary>
        /// 获取数据库的注册数量
        /// </summary>
        /// <returns></returns>
        public static int GetDbQuantity()
        {
            return IdleBusProvider.Instance?.Quantity ?? 0;
        }

        /// <summary>
        /// 获取注册数据库所有的Key
        /// </summary>
        /// <returns></returns>
        public static List<string> GetDbKeys()
        {
            return IdleBusProvider.Instance.GetKeys().ToList();
        }


        /// <summary>
        /// 通过命名空间得到所有要创建的实体类.
        /// </summary>
        /// <typeparam name="IEntity"></typeparam>
        /// <param name="entitiesFullName">根据需要调整 entitiesFullName 下的命名空间值</param>
        /// <returns></returns>
        public static Type[] GetTypesByNameSpace<IEntity>()
        {
            List<Type> tableAssembies = new List<Type>();

            foreach (Type type in Assembly.GetAssembly(typeof(IEntity)).GetExportedTypes())
                tableAssembies.Add(type);

            return tableAssembies.ToArray();
        }
    }

    internal class DbInfoByAttribute
    {
        public string Name { get; set; }

        public DateTimeSeparateImpl DateTimeSeparate { get; set; }
    }
}