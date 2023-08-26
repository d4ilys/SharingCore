using System;
using System.Collections.Generic;
using System.Text;
using SharingCore.Assemble;
using Microsoft.AspNetCore.Builder;
using SharingCore.Context;

namespace SharingCore.Extensions
{
    public static class IApplicationBuilderExtension
    {
        /// <summary>
        /// 对SharingCore中间件一些配置
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="func">通过HTTP管道获取每次用户请求所属的租户</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSharingCore(this IApplicationBuilder app, Func<string> func)
        {
            app.Use(async (context, next) =>
            {
                var tenant = func?.Invoke();
                //配置全局租户
                if (tenant != null)
                    TenantContext.SetTenant(tenant);
                await next();
            });
            return app;
        }
    }
}