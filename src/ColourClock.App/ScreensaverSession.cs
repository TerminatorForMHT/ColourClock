using ColourClock.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using TheColourClock.Core;

namespace ColourClock.App;

/// <summary>
/// Full screen screen saver: one borderless topmost window per monitor.
/// Any keyboard or mouse input exits (classic screen saver behaviour).
/// </summary>
public sealed class ScreensaverSession
{
    private const long InputGraceMs = 1500;

    private readonly DisplayMode _mode;
    private readonly List<Window> _windows = new();
    private DispatcherTimer? _idleTimer;
    private uint _baselineInputTick;
    private long _startedTick;
    private bool _cursorHidden;
    private bool _isExiting;

    public ScreensaverSession(DisplayMode mode)
    {
        _mode = mode;
    }

    public void Start()
    {
        _startedTick = Environment.TickCount64;

        var info = new NativeMethods.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };
        if (NativeMethods.GetLastInputInfo(ref info))
        {
            _baselineInputTick = info.dwTime;
        }

        NativeMethods.ShowCursor(false);
        _cursorHidden = true;

        // Indexed access: enumerating DisplayArea.FindAll() throws an invalid cast
        // in this WinRT projection.
        var areas = DisplayArea.FindAll();
        for (var i = 0; i < areas.Count; i++)
        {
            CreateWindow(areas[i]);
        }

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _idleTimer.Tick += (_, _) => CheckForInput();
        _idleTimer.Start();
    }

    private void CreateWindow(DisplayArea area)
    {
        var window = new Window { Content = new ClockVisual(_mode, showBorder: false) };
        var handle = Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);

        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        // Keep the saver out of Alt+Tab and the task bar.
        NativeMethods.AddToolWindowStyle(handle);

        window.AppWindow.MoveAndResize(area.OuterBounds);
        window.Closed += (_, _) => OnWindowClosed(window);
        _windows.Add(window);
        window.Activate();

        // DWM still paints a thin accent frame inside the window edges otherwise.
        NativeMethods.RemoveWindowFrame(handle);
    }

    private void CheckForInput()
    {
        var info = new NativeMethods.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };
        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            return;
        }

        if (Environment.TickCount64 - _startedTick < InputGraceMs)
        {
            // Ignore input during the fade in, like the original projector.
            _baselineInputTick = info.dwTime;
            return;
        }

        if (info.dwTime != _baselineInputTick)
        {
            RequestExit();
        }
    }

    private void OnWindowClosed(Window window)
    {
        _windows.Remove(window);

        if (_windows.Count == 0 && !_isExiting)
        {
            _isExiting = true;
            _idleTimer?.Stop();
            Application.Current.Exit();
        }
    }

    public void RequestExit()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _idleTimer?.Stop();

        if (_cursorHidden)
        {
            NativeMethods.ShowCursor(true);
            _cursorHidden = false;
        }

        foreach (var window in _windows.ToArray())
        {
            window.Close();
        }

        _windows.Clear();
        Application.Current.Exit();
    }
}
