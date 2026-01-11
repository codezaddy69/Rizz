using DJMixMaster;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<RizzApplication>();

        var app = new RizzApplication(logger);
        app.Initialize();

        Console.WriteLine("RizzApplication initialized. Press any key to shutdown.");
        Console.ReadKey();

        app.Shutdown();
        Console.WriteLine("Shutdown complete.");
    }
}