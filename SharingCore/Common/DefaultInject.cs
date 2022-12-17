using System;
using System.Collections.Generic;
using System.Text;
using Daily.SharingCore.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Daily.SharingCore.Common.DefaultInject))]
namespace Daily.SharingCore.Common
{
    internal class DefaultInject : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                // 存储配置对象
                InternalApp.Configuration = context.Configuration;

                services.AddHostedService<GenericHostedService>();

                services.AddHttpContextAccessor();
            });
        }
    }
}