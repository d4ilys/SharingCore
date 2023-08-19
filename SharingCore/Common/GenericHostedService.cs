using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharingCore.Common
{
    public class GenericHostedService : IHostedService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        public GenericHostedService(IHost host)
        {
            // 存储根服务
            SharingCoreUtils.Services = host.Services;

            //配置
            SharingCoreUtils.Configuration = SharingCoreUtils.Services.GetService<IConfiguration>();

        }

        /// <summary>
        /// 监听主机启动
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 监听主机停止
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}