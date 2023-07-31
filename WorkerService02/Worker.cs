using SharingCore.Extensions;

namespace WorkerService02
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var order = "order".GetFreeSql();
            var money = "money".GetFreeSql();

            order.CodeFirst.SyncStructure<Person>();
            var persons = order.Select<Person>().ToList();

            money.CodeFirst.SyncStructure<Students>();
            var students = money.Select<Students>().ToList();
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int IsDelete
        { get; set; }
    }

    public class Students
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int IsDelete { get; set; }
    }
}