﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FreeSql.SharingCore.Assemble;
using FreeSql.SharingCore.Context;
using FreeSql.SharingCore.MultiDatabase.Transcation;
using FreeSql.SharingCore.MultiDatabase.Wrapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FreeSql.SharingCore.Common
{
    /// <summary>
    /// SharingCore工具类
    /// </summary>
    public partial class SharingCoreUtils
    {
        private static readonly HashSet<DbInfoByAttribute> DbInfosCache = new HashSet<DbInfoByAttribute>();

        internal static void InitMethodCache()
        {
            var type = typeof(SharingCoreDbs);
            var entryAssembly = Assembly.GetEntryAssembly();
            var assembly = new List<Assembly>() { entryAssembly };
            var baseAssembly = Options?.ExtensionMethodsAssembly;
            if (baseAssembly.Any())
            {
                assembly.AddRange(baseAssembly);
            }

            GetExtensionMethods(assembly, type);
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
                return DbInfosCache.Any(m => name.ToLower().StartsWith(m.Name.ToLower()));
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
                return DbInfosCache.FirstOrDefault(m => m.Name.ToLower() == name.ToLower())?.DateTimeSeparate;
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerable<string> GetDbNamesByColumnValueRange(string name, string tenant,
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
                    var dbInfoByAttribute = new DbInfoByAttribute();

                    //得到扩展方法的返回值
                    var res = item.method.Invoke(null, new object?[]
                    {
                        Activator.CreateInstance(extendedType)
                    });

                    dbInfoByAttribute.Name = res.ToString();

                    var attribute = item.method.GetCustomAttribute<DatabaseAttribute>();

                    //先看看配置文件有没有分库规则
                    if (attribute == null)
                    {
                        var rule = DatabaseConfig.SeparateRules.FirstOrDefault(s =>
                            s.Name == dbInfoByAttribute.Name);
                        if (rule != null)
                        {
                            attribute = new DatabaseAttribute()
                            {
                                Name = rule.Template,
                                Separate = rule.Separate
                            };
                        }
                    }

                    //如果没有标记特性默认是扩展方法的名称
                    if (attribute != null)
                    {
                        //如果分库
                        if (!string.IsNullOrWhiteSpace(attribute.Separate))
                        {
                            dbInfoByAttribute.DateTimeSeparate = attribute.ParseSeparate();
                        }
                    }


                    DbInfosCache.Add(dbInfoByAttribute);
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
        /// <returns></returns>
        public static Type[] GetTypesByNameSpace<IEntity>()
        {
            List<Type> tableAssembies = new List<Type>();

            foreach (Type type in Assembly.GetAssembly(typeof(IEntity)).GetExportedTypes())
                tableAssembies.Add(type);

            return tableAssembies.ToArray();
        }

        /// <summary>
        /// 向FreeSql管理器追加新的实例
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static void IdleBusAppend(SharingCoreDbConfig config)
        {
            IdleBusProvider.Register(config, Options);
            DatabaseConfig.DatabaseInfo.AddRange(config.DatabaseInfo);
            DatabaseConfig.SeparateRules.AddRange(config.SeparateRules);
        }

        /// <summary>
        /// FreeSql管理器删除指定实例
        /// </summary>
        /// <returns></returns>
        public static void IdleBusRemove(params string[] keys)
        {
            foreach (var key in keys)
            {
                IdleBusProvider.Instance?.TryRemove(key);
            }
        }

        /// <summary>
        /// 获取数据库的注册数量
        /// </summary>
        /// <returns></returns>
        public static List<string> IdleBusRegisterInfo()
        {
            return IdleBusProvider.Instance!.GetKeys().ToList();
        }

        /// <summary>
        /// 获取数据库的注册数量
        /// </summary>
        /// <returns></returns>
        public static int IdleBusRegisterCount()
        {
            return IdleBusProvider.Instance?.Quantity ?? 0;
        }

        internal static void LogWarning(string message)
        {
            try
            {
                var logger = Services.GetService<ILogger<SharingCoreUtils>>();
                if (logger != null) logger.LogWarning(message);
            }
            catch
            {
                // ignored
            }
        }

        internal static void LogInformation(string message)
        {
            try
            {
                var logger = Services.GetService<ILogger<SharingCoreUtils>>();
                if (logger != null) logger.LogInformation(message);
            }
            catch
            {
                // ignored
            }
        }

        internal static void LogError(string message)
        {
            try
            {
                var logger = Services.GetService<ILogger<SharingCoreUtils>>();
                if (logger != null) logger.LogError(message);
            }
            catch
            {
                // ignored
            }
        }
    }

    internal class DbInfoByAttribute
    {
        public string Name { get; set; }

        public DateTimeSeparateImpl DateTimeSeparate { get; set; }
    }
}