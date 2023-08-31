using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(FreeSql.SharingCore.Common.DefaultInject))]
namespace FreeSql.SharingCore.Common
{
    internal class DefaultInject : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                // 存储配置对象
                SharingCoreUtils.Configuration = context.Configuration;

                services.AddHostedService<GenericHostedService>();

                services.AddHttpContextAccessor();
            });
        }
    }
}