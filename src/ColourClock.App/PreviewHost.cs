using System.Runtime.InteropServices;
using TheColourClock.Core;

namespace ColourClock.App;

/// <summary>
/// /p preview: the Display Properties dialog gives us a parent HWND; WinUI cannot
/// host a child window there, so the preview is painted with GDI at that size.
/// </summary>
internal static class PreviewHost
{
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;

    private const int WmDestroy = 0x0002;
    private const int WmPaint = 0x000F;
    private const int WmEraseBkgnd = 0x0014;
    private const int WmTimer = 0x0113;

    private const string ClassName = "ColourClockPreviewWindow";
    private const string FontFace = "Jost* 500 Medium";

    private static NativeMethods.WndProc? _windowProc;

    // A cross-process child window inherits the DPI context of the dialog that hosts
    // it, so GDI scales font heights by LOGPIXELSY/96.  Sizes are compensated back to
    // em pixels on the paint DC, otherwise the text overflows the preview.
    private static double _fontScale = 1.0;

    public static void Run(IntPtr parent)
    {
        NativeMethods.RECT rect;
        if (parent == IntPtr.Zero || !NativeMethods.GetClientRect(parent, out rect))
        {
            return;
        }

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        SettingsStore.Load();

        var instance = NativeMethods.GetModuleHandleW(null);
        _windowProc = WindowProcedure;

        var windowClass = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = _windowProc,
            hInstance = instance,
            lpszClassName = ClassName,
        };

        NativeMethods.RegisterClassExW(ref windowClass);

        var handle = NativeMethods.CreateWindowExW(0, ClassName, string.Empty, WsChild | WsVisible,
            0, 0, rect.Width, rect.Height, parent, IntPtr.Zero, instance, IntPtr.Zero);

        if (handle == IntPtr.Zero)
        {
            return;
        }

        LoadFont();

        NativeMethods.SetTimer(handle, new UIntPtr(1), 1000, IntPtr.Zero);
        NativeMethods.InvalidateRect(handle, IntPtr.Zero, false);

        while (true)
        {
            var result = NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0);
            if (result == 0 || result == -1)
            {
                break;
            }

            NativeMethods.TranslateMessage(ref message);
            NativeMethods.DispatchMessage(ref message);
        }
    }

    private static void LoadFont()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Jost.ttf");
            if (File.Exists(path))
            {
                NativeMethods.AddFontResourceExW(path, NativeMethods.FR_PRIVATE, IntPtr.Zero);
            }
        }
        catch
        {
            // Preview falls back to a system font.
        }
    }

    private static IntPtr WindowProcedure(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WmPaint:
                Paint(hWnd);
                return IntPtr.Zero;

            case WmEraseBkgnd:
                return new IntPtr(1);

            case WmTimer:
                NativeMethods.InvalidateRect(hWnd, IntPtr.Zero, false);
                return IntPtr.Zero;

            case WmDestroy:
                NativeMethods.KillTimer(hWnd, new UIntPtr(1));
                NativeMethods.PostQuitMessage(0);
                return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProcW(hWnd, message, wParam, lParam);
    }

    private static void Paint(IntPtr hWnd)
    {
        var paint = new NativeMethods.PAINTSTRUCT();
        var dc = NativeMethods.BeginPaint(hWnd, out paint);

        try
        {
            NativeMethods.RECT rect;
            if (NativeMethods.GetClientRect(hWnd, out rect) && rect.Width > 0 && rect.Height > 0)
            {
                Render(dc, rect.Width, rect.Height, DateTime.Now);
            }
        }
        finally
        {
            NativeMethods.EndPaint(hWnd, ref paint);
        }
    }

    private static void Render(IntPtr dc, int width, int height, DateTime now)
    {
        var mode = SettingsStore.Current.Mode;
        var colour = ClockMath.GetColor(now);
        var background = ToColorRef(colour.R, colour.G, colour.B);

        Fill(dc, 0, 0, width, height, background);

        var dpi = NativeMethods.GetDeviceCaps(dc, 88);
        _fontScale = dpi > 0 ? 96.0 / dpi : 1.0;

        var scale = ClockLayout.GetHolderScale(width);
        var centerX = (int)Math.Round(width / 2.0);
        var centerY = (int)Math.Floor(height / 2.0);

        var mainSize = (int)Math.Round(ClockLayout.ClockFontSize * scale);
        var texts = ClockMath.GetDisplayTexts(mode, now);
        var mainColor = Blend(background, 0.7529);

        if (mode == DisplayMode.Both)
        {
            DrawCentered(dc, texts.Primary, mainSize, mainColor,
                centerX, centerY + (int)Math.Round(ClockLayout.BaselineAt(ClockLayout.BothClockTop, ClockLayout.ClockFontSize) * scale));

            DrawCentered(dc, texts.Secondary ?? string.Empty, mainSize, mainColor,
                centerX, centerY + (int)Math.Round(ClockLayout.BaselineAt(ClockLayout.BothColourTop, ClockLayout.ClockFontSize) * scale));
        }
        else
        {
            DrawCentered(dc, texts.Primary, mainSize, mainColor,
                centerX, centerY + (int)Math.Round(ClockLayout.BaselineAt(ClockLayout.SingleTextTop, ClockLayout.ClockFontSize) * scale));
        }

        // Divider
        var dividerWidth = (int)Math.Round(ClockLayout.DividerWidth * scale);
        var dividerY = centerY + (int)Math.Round((mode == DisplayMode.Both ? ClockLayout.BothDividerTop : ClockLayout.SingleDividerTop) * scale);
        Fill(dc, centerX - dividerWidth / 2, dividerY, dividerWidth, 1, Blend(background, 0.55));

        // Brand block: the original hides the subhead when the stage is 400px or narrower.
        DrawAt(dc, "The Colour Clock.", 12, Blend(background, 0.698), 17, 27);

        if (width > ClockLayout.SubheadHideBreakpoint)
        {
            DrawAt(dc, "Representing time as a hexadecimal colour value.", 12, Blend(background, 0.447), 17, 42);
        }
    }

    private static void DrawCentered(IntPtr dc, string text, int size, uint color, int centerX, int baselineY)
    {
        if (size <= 0)
        {
            return;
        }

        var font = CreateFont(size);
        if (font == IntPtr.Zero)
        {
            return;
        }

        var previous = NativeMethods.SelectObject(dc, font);

        NativeMethods.SIZE extent;
        NativeMethods.GetTextExtentPoint32W(dc, text, text.Length, out extent);

        NativeMethods.SetTextColor(dc, color);
        NativeMethods.SetBkMode(dc, 1);
        NativeMethods.SetTextAlign(dc, NativeMethods.TaBaseline);
        NativeMethods.TextOutW(dc, centerX - extent.cx / 2, baselineY, text, text.Length);

        NativeMethods.SelectObject(dc, previous);
        NativeMethods.DeleteObject(font);
    }

    private static void DrawAt(IntPtr dc, string text, int size, uint color, int left, int baselineY)
    {
        var font = CreateFont(size);
        if (font == IntPtr.Zero)
        {
            return;
        }

        var previous = NativeMethods.SelectObject(dc, font);

        NativeMethods.SetTextColor(dc, color);
        NativeMethods.SetBkMode(dc, 1);
        NativeMethods.SetTextAlign(dc, NativeMethods.TaBaseline);
        NativeMethods.TextOutW(dc, left, baselineY, text, text.Length);

        NativeMethods.SelectObject(dc, previous);
        NativeMethods.DeleteObject(font);
    }

    private static IntPtr CreateFont(int size)
    {
        var height = Math.Max(1, (int)Math.Round(size * _fontScale));
        var font = NativeMethods.CreateFontW(-height, 0, 0, 0, 500, 0, 0, 0, 1, 0, 0, 4, 0, FontFace);
        if (font == IntPtr.Zero || font == new IntPtr(-1))
        {
            return IntPtr.Zero;
        }

        return font;
    }

    private static void Fill(IntPtr dc, int left, int top, int width, int height, uint color)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var brush = NativeMethods.CreateSolidBrush(color);
        if (brush == IntPtr.Zero)
        {
            return;
        }

        var rect = new NativeMethods.RECT { Left = left, Top = top, Right = left + width, Bottom = top + height };
        NativeMethods.FillRect(dc, ref rect, brush);
        NativeMethods.DeleteObject(brush);
    }

    /// <summary>Blends white over the stage colour, producing an opaque GDI colour.</summary>
    private static uint Blend(uint background, double alpha)
    {
        var r = BlendChannel((int)(background & 0xFF), alpha);
        var g = BlendChannel((int)((background >> 8) & 0xFF), alpha);
        var b = BlendChannel((int)((background >> 16) & 0xFF), alpha);
        return ToColorRef(r, g, b);
    }

    private static int BlendChannel(int channel, double alpha)
        => (int)Math.Round(channel * (1 - alpha) + 255 * alpha);

    private static uint ToColorRef(int r, int g, int b)
        => (uint)((b << 16) | (g << 8) | r);
}
