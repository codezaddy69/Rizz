using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;

namespace DJMixMaster;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Parse command-line options
        var options = ParseArgs(args);

        var app = new App();
        var mainWindow = new MainWindow(options);
        app.MainWindow = mainWindow;
        app.Run(mainWindow);
    }

    private static AppOptions ParseArgs(string[] args)
    {
        var options = new AppOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--asio-device":
                    if (i + 1 < args.Length) options.AsioDevice = args[++i];
                    break;
                case "--sample-rate":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int rate)) options.SampleRate = rate;
                    break;
                case "--verbose":
                    options.LogLevel = LogLevel.Debug;
                    break;
                case "--minimal":
                    options.LogLevel = LogLevel.Error;
                    break;
                case "--test-tone":
                    options.TestTone = true;
                    break;
                case "--file":
                    if (i + 1 < args.Length) options.AutoLoadFile = args[++i];
                    break;
                case "--run-test":
                    options.RunTest = true;
                    break;
                case "--debug":
                    options.LogLevel = LogLevel.Information;
                    break;
                case "--debug-full":
                    options.LogLevel = LogLevel.Debug;
                    break;
            }
        }
        return options;
    }

    public class AppOptions
    {
        public string? AsioDevice { get; set; }
        public int SampleRate { get; set; } = 44100;
        public LogLevel LogLevel { get; set; } = LogLevel.Error;
        public bool TestTone { get; set; }
        public string? AutoLoadFile { get; set; }
        public bool RunTest { get; set; }
    }

    public enum LogLevel { Debug, Information, Warning, Error }

    private StreamWriter? appLogWriter;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        appLogWriter = new StreamWriter("app_error.log", true);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Exception? ex = args.ExceptionObject as Exception;
#pragma warning disable CS8600 // ToString() on Exception returns non-null string
            string message = ex != null ? ex.ToString() : args.ExceptionObject.ToString();
#pragma warning restore CS8600
            appLogWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} FATAL: Unhandled exception: {message}");
            appLogWriter?.Flush();
            Console.WriteLine($"Fatal error: {message}");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            appLogWriter?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR: Dispatcher exception: {args.Exception}");
            appLogWriter?.Flush();
            Console.WriteLine($"Application error: {args.Exception.Message}");
            args.Handled = true;
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        appLogWriter?.Dispose();
        base.OnExit(e);
    }
}

