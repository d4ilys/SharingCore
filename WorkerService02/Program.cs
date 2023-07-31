using SharingCore;
using SharingCore.Extensions;

namespace WorkerService02
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .InjectSharingCore()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSharingCore(options =>
                    {
                        var oneselfCustom = new CustomDatabaseSettings();
                        oneselfCustom.FreeSqlFilter<FreeSqlFilter>(f => f.IsDelete == 0);
                        options.CustomAllDatabaseSettings = oneselfCustom;
                    });
                })
                .Build();

            host.Run();
        }
    }
}