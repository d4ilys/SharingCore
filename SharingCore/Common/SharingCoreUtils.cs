﻿using Microsoft.Extensions.DependencyInjection;
using SharingCore.MultiDatabase.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharingCore.Assemble;

namespace SharingCore.Common
{
    /// <summary>
    /// SharingCore工具类
    /// </summary>
    public partial class SharingCoreUtils
    {
        private static HashSet<DbInfoByAttribute> dbInfosCache = new HashSet<DbInfoByAttribute>();

        internal static void InitMehtodCache()
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

        /// <summary>
        /// 通过SharingCoreDbs中的扩展方法特性获取分库对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
    }

    internal class DbInfoByAttribute
    {
        public string Name { get; set; }

        public DateTimeSeparateImpl DateTimeSeparate { get; set; }
    }
}