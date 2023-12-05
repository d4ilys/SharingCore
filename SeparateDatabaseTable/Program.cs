using System.Reflection;
using FreeSql.SharingCore.Extensions;
using Newtonsoft.Json;

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