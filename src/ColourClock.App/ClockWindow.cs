using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using ColourClock.App.Views;
using TheColourClock.Core;
using Windows.Graphics;

namespace ColourClock.App;

/// <summary>Normal windowed clock: draggable, F11 full screen, Esc closes.</summary>
public sealed class ClockWindow
{
    private const int VkEscape = 0x1B;
    private const int VkF11 = 0x7A;
    private const int VkOemComma = 0xBC;
    private const int VkControl = 0x11;

    private readonly Window _window;
    private readonly ClockVisual _visual;
    private readonly IntPtr _handle;
    private NativeMethods.SubclassProc? _windowProc;
    private RectInt32 _restoreBounds;
    private bool _isFullScreen;
    private bool _isClosing;

    public ClockWindow(DisplayMode mode)
    {
        _visual = new ClockVisual(mode);
        _window = new Window { Title = "The Colour Clock", Content = _visual };
        _handle = Win32Interop.GetWindowFromWindowId(_window.AppWindow.Id);

        if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        _window.AppWindow.ResizeClient(ToPixels(960, 600, _handle));
        CenterOnPrimary();

        _visual.PointerPressed += OnDrag;

        _windowProc = OnWindowProc;
        NativeMethods.SetWindowSubclass(_handle, _windowProc, 1, IntPtr.Zero);

        _window.Closed += (_, _) => _isClosing = true;
    }

    public void Show() => _window.Activate();

    private static SizeInt32 ToPixels(int width, int height, IntPtr handle)
    {
        var dpi = NativeMethods.GetDpiForWindow(handle);
        var scale = dpi <= 0 ? 1.0 : dpi / 96.0;
        return new SizeInt32((int)Math.Round(width * scale), (int)Math.Round(height * scale));
    }

    private void CenterOnPrimary()
    {
        var area = DisplayArea.GetFromWindowId(_window.AppWindow.Id, DisplayAreaFallback.Primary);
        var size = _window.AppWindow.Size;
        var x = area.WorkArea.X + (area.WorkArea.Width - size.Width) / 2;
        var y = area.WorkArea.Y + (area.WorkArea.Height - size.Height) / 2;
        _window.AppWindow.Move(new PointInt32(x, y));
    }

    private void OnDrag(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(_visual).Properties.IsLeftButtonPressed)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(_handle, NativeMethods.WM_NCLBUTTONDOWN, new IntPtr(NativeMethods.HTCAPTION), IntPtr.Zero);
    }

    private IntPtr OnWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
    {
        switch (message)
        {
            case NativeMethods.WM_KEYDOWN:
            {
                var key = (int)wParam;

                if (key == VkF11)
                {
                    ToggleFullScreen();
                    return IntPtr.Zero;
                }

                if (key == VkEscape)
                {
                    if (_isFullScreen)
                    {
                        ToggleFullScreen();
                    }
                    else
                    {
                        Close();
                    }

                    return IntPtr.Zero;
                }

                if (key == VkOemComma && NativeMethods.GetKeyState(VkControl) < 0)
                {
                    new SettingsWindow().Show();
                    return IntPtr.Zero;
                }

                break;
            }

            case NativeMethods.WM_DESTROY:
                if (_windowProc is not null)
                {
                    NativeMethods.RemoveWindowSubclass(hWnd, _windowProc, id);
                }

                break;
        }

        return NativeMethods.DefSubclassProc(hWnd, message, wParam, lParam);
    }

    private void ToggleFullScreen()
    {
        var presenter = _window.AppWindow.Presenter as OverlappedPresenter;

        if (_isFullScreen)
        {
            _isFullScreen = false;
            _window.AppWindow.MoveAndResize(_restoreBounds);

            if (presenter is not null)
            {
                presenter.IsAlwaysOnTop = false;
            }

            return;
        }

        var current = _window.AppWindow.Position;
        var size = _window.AppWindow.Size;
        _restoreBounds = new RectInt32(current.X, current.Y, size.Width, size.Height);

        var area = DisplayArea.GetFromWindowId(_window.AppWindow.Id, DisplayAreaFallback.Primary);
        _isFullScreen = true;
        _window.AppWindow.MoveAndResize(area.OuterBounds);

        if (presenter is not null)
        {
            presenter.IsAlwaysOnTop = true;
        }
    }

    private void Close()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _window.Close();
    }
}
