using SharingCore.Extensions;

namespace SeparateDatabaseTable
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSharingCore();
                })
                .Build();

            host.Run();
        }
    }
}