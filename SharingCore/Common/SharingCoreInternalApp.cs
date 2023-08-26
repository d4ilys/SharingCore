using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using SharingCore.Extensions;

namespace SharingCore.Common
{
    public partial class SharingCoreUtils
    {
        /// <summary>
        /// 根服务
        /// </summary>
        public static IServiceProvider Services { get; internal set; }

        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration { get; internal set; }

        /// <summary>
        /// 整体配置
        /// </summary>
        public static SharingCoreOptions Options { get; internal set; }

        /// <summary>
        /// 所有数据库配置
        /// </summary>
        public static SharingCoreDbConfig DatabaseConfig { get; internal set; }
    }
}