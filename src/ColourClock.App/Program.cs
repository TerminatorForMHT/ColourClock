using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ColourClock.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            var options = LaunchOptions.Parse(args);
            LaunchOptions.Current = options;

            if (options.Mode == LaunchMode.Preview)
            {
                PreviewHost.Run(options.ParentHandle);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("AppDomain", e.ExceptionObject as Exception);

            WinRT.ComWrappersSupport.InitializeComWrappers();

            Application.Start(initializationCallbackParams =>
            {
                var queue = DispatcherQueue.GetForCurrentThread();
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(queue));
                new App();
            });
        }
        catch (Exception exception)
        {
            Log("Main", exception);
            throw;
        }
    }

    internal static void Log(string stage, Exception? exception)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "cc_startup.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {stage}: {exception}\r\n\r\n");
        }
        catch
        {
            // Diagnostics only.
        }
    }
}
