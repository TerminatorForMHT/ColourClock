using Microsoft.UI.Xaml;
using TheColourClock.Core;

namespace ColourClock.App;

public partial class App : Application
{
    private ScreensaverSession? _screensaver;
    private ClockWindow? _clockWindow;
    private SettingsWindow? _settingsWindow;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => Program.Log("App", e.Exception);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            SettingsStore.Load();

            switch (LaunchOptions.Current.Mode)
            {
                case LaunchMode.Screensaver:
                {
                    var session = new ScreensaverSession(SettingsStore.Current.Mode);
                    _screensaver = session;
                    session.Start();
                    break;
                }

                case LaunchMode.Settings:
                    // WinUI closes the window once its managed wrapper is collected.
                    _settingsWindow = new SettingsWindow();
                    _settingsWindow.Show();
                    break;

                default:
                    _clockWindow = new ClockWindow(SettingsStore.Current.Mode);
                    _clockWindow.Show();
                    break;
            }
        }
        catch (Exception exception)
        {
            Program.Log("OnLaunched", exception);
            throw;
        }
    }
}
