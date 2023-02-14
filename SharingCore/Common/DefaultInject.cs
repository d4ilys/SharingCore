using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(SharingCore.Common.DefaultInject))]
namespace SharingCore.Common
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