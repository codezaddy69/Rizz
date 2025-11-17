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
    public static void Main()
    {
        var app = new App();
        var mainWindow = new MainWindow();
        app.MainWindow = mainWindow;
        app.Run(mainWindow);
    }

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

