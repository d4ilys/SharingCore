using SharingCore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharingCore.Extensions
{
    public static class Hosting
    {
        public static IHostBuilder InjectSharingCore(this IHostBuilder builder)
        {
            builder.ConfigureServices((hostContext, services) =>
            {
                // 存储配置对象
                SharingCoreUtils.Configuration = hostContext.Configuration;

                services.AddHostedService<GenericHostedService>();

                services.AddHttpContextAccessor();

            });
            return builder;
            // 存储根服务
        }
    }
}

