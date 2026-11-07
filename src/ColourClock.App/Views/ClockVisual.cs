using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TheColourClock.Core;
using Windows.Foundation;
using Windows.UI;

namespace ColourClock.App.Views;

/// <summary>
/// The full screen visual: animated stage colour, vignette, 1px border and the
/// three text layers, reproducing colour_clock_screen.as / MainTimeline.as.
/// </summary>
public sealed partial class ClockVisual : UserControl
{
    private readonly DispatcherTimer _timer = new();
    private readonly SolidColorBrush _backgroundBrush = new();

    private DisplayMode _mode;
    private RgbColor _current;
    private long _lastSecond;
    private bool _isReflowPending;

    public ClockVisual(DisplayMode mode, bool showBorder = true)
    {
        InitializeComponent();
        _mode = mode;

        BorderLayer.Visibility = showBorder ? Visibility.Visible : Visibility.Collapsed;

        BackgroundRect.Fill = _backgroundBrush;

        var now = DateTime.Now;
        _current = ClockMath.GetColor(now);
        _lastSecond = now.Ticks / TimeSpan.TicksPerSecond;
        _backgroundBrush.Color = Color.FromArgb(255, _current.R, _current.G, _current.B);

        ApplyTexts(now);
        ApplyLayout();

        Loaded += (_, _) => Reflow();
        SizeChanged += (_, _) => Reflow();
        SettingsStore.Changed += OnSettingsChanged;

        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public DisplayMode Mode => _mode;

    public void SetMode(DisplayMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
        ApplyTexts(DateTime.Now);
        ApplyLayout();
        Reflow();
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => SetMode(SettingsStore.Current.Mode);

    private void OnTimerTick(object? sender, object e)
    {
        var now = DateTime.Now;
        var second = now.Ticks / TimeSpan.TicksPerSecond;

        if (second == _lastSecond)
        {
            return;
        }

        // The colour is sampled once per second, so the background is exactly the
        // colour whose hexadecimal code is on screen - no fading between the two.
        _lastSecond = second;
        _current = ClockMath.GetColor(now);
        _backgroundBrush.Color = Color.FromArgb(255, _current.R, _current.G, _current.B);
        ApplyTexts(now);
    }

    private void ApplyTexts(DateTime now)
    {
        var texts = ClockMath.GetDisplayTexts(_mode, now);
        ClockText.Text = texts.Primary;

        if (_mode == DisplayMode.Both)
        {
            ColourText.Text = texts.Secondary ?? string.Empty;
        }
    }

    private void ApplyLayout()
    {
        var both = _mode == DisplayMode.Both;

        UiHelpers.PlaceAtBaseline(ClockText, ClockLayout.ClockTextLeft,
            ClockLayout.BaselineAt(both ? ClockLayout.BothClockTop : ClockLayout.SingleTextTop, ClockLayout.ClockFontSize));

        ColourText.Visibility = both ? Visibility.Visible : Visibility.Collapsed;

        if (both)
        {
            UiHelpers.PlaceAtBaseline(ColourText, ClockLayout.ClockTextLeft,
                ClockLayout.BaselineAt(ClockLayout.BothColourTop, ClockLayout.ClockFontSize));
        }

        Canvas.SetLeft(Divider, -ClockLayout.DividerWidth / 2);
        Canvas.SetTop(Divider, both ? ClockLayout.BothDividerTop : ClockLayout.SingleDividerTop);

        Reflow();
    }

    private void Reflow()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || _isReflowPending)
        {
            return;
        }

        // Canvas.SetLeft/SetTop with fractional values on a 0x0 Canvas is cheap, but
        // re-entrancy from SizeChanged must not recurse.
        _isReflowPending = true;

        var scale = ClockLayout.GetHolderScale(width);
        HolderScale.ScaleX = scale;
        HolderScale.ScaleY = scale;
        Canvas.SetLeft(Holder, Math.Round(width / 2));
        Canvas.SetTop(Holder, Math.Floor(height / 2));

        // Brand block: text baselines derived from the SWF text records.
        UiHelpers.PlaceAtBaseline(TitleText, ClockLayout.TitleLeft, 27);
        UiHelpers.PlaceAtBaseline(SubheadText, ClockLayout.SubheadLeft, 42);
        SubheadText.Visibility = width <= ClockLayout.SubheadHideBreakpoint
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Border: four 1px edges inset by 5px.
        Canvas.SetLeft(BorderTop, 5);
        Canvas.SetTop(BorderTop, 5);
        BorderTop.Width = Math.Max(0, width - 11);

        Canvas.SetLeft(BorderBottom, 5);
        Canvas.SetTop(BorderBottom, height - 6);
        BorderBottom.Width = Math.Max(0, width - 11);

        Canvas.SetLeft(BorderLeft, 5);
        Canvas.SetTop(BorderLeft, 5);
        BorderLeft.Height = Math.Max(0, height - 11);

        Canvas.SetLeft(BorderRight, width - 6);
        Canvas.SetTop(BorderRight, 5);
        BorderRight.Height = Math.Max(0, height - 11);

        // Radial DARKEN vignette, gradient box = max(W,H) + 398, centred.
        if (Vignette.Fill is RadialGradientBrush brush)
        {
            var radius = (Math.Max(width, height) + 398) / 2.0;
            var center = new Point(width / 2, height / 2);
            brush.Center = center;
            brush.GradientOrigin = center;
            brush.RadiusX = radius;
            brush.RadiusY = radius;
        }

        _isReflowPending = false;
    }
}
