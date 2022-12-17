using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Daily.SharingCore.Common
{
    public class InternalApp
    {
        /// <summary>
        /// 根服务
        /// </summary>
        public static IServiceProvider RootServices;

        /// <summary>
        /// 配置对象
        /// </summary>
        public static IConfiguration Configuration;

    }
}
