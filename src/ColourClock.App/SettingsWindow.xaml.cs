using ColourClock.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using TheColourClock.Core;
using Windows.Graphics;
using Windows.UI;

namespace ColourClock.App;

/// <summary>
/// 400x400 settings dialog - the AS2 settings scene rebuilt without Flash.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private const double RowTop = 174;
    private const double RowPitch = 39;
    private const double RowHeight = 20;

    private const int VkEscape = 0x1B;

    private static readonly string[] OptionLabels = { "CLOCK", "COLOUR", "CLOCK & COLOUR" };

    private readonly List<OptionRow> _rows = new();
    private readonly IntPtr _handle;
    private NativeMethods.SubclassProc? _windowProc;
    private OptionRow? _hovered;

    private sealed class OptionRow
    {
        public required DisplayMode Mode { get; init; }
        public required Ellipse Skin { get; init; }
        public required Ellipse Dot { get; init; }
        public required TextBlock Label { get; init; }
    }

    public SettingsWindow()
    {
        InitializeComponent();

        _handle = Win32Interop.GetWindowFromWindowId(AppWindow.Id);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        var dpi = NativeMethods.GetDpiForWindow(_handle);
        var scale = dpi <= 0 ? 1.0 : dpi / 96.0;
        AppWindow.ResizeClient(new SizeInt32((int)Math.Round(400 * scale), (int)Math.Round(400 * scale)));

        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var size = AppWindow.Size;
        AppWindow.Move(new PointInt32(
            area.WorkArea.X + (area.WorkArea.Width - size.Width) / 2,
            area.WorkArea.Y + (area.WorkArea.Height - size.Height) / 2));

        BuildOptions();
        LayoutChrome();
        UpdateRows();

        Root.PointerPressed += OnDrag;

        CloseButton.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            Close();
        };
        CloseButton.PointerEntered += (_, _) => CloseButton.Opacity = 0.9;
        CloseButton.PointerExited += (_, _) => CloseButton.Opacity = 0.55;

        _windowProc = OnWindowProc;
        NativeMethods.SetWindowSubclass(_handle, _windowProc, 2, IntPtr.Zero);
        Closed += (_, _) =>
        {
            if (_windowProc is not null)
            {
                NativeMethods.RemoveWindowSubclass(_handle, _windowProc, 2);
            }
        };
    }

    public void Show() => Activate();

    private void OnDrag(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled || !e.GetCurrentPoint(Root).Properties.IsLeftButtonPressed)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(_handle, NativeMethods.WM_NCLBUTTONDOWN, new IntPtr(NativeMethods.HTCAPTION), IntPtr.Zero);
    }

    private IntPtr OnWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
    {
        if (message == NativeMethods.WM_KEYDOWN && (int)wParam == VkEscape)
        {
            Close();
            return IntPtr.Zero;
        }

        return NativeMethods.DefSubclassProc(hWnd, message, wParam, lParam);
    }

    private void BuildOptions()
    {
        for (var i = 0; i < OptionLabels.Length; i++)
        {
            var mode = (DisplayMode)(i + 1);
            var rowCanvas = new Canvas { Width = 400, Height = RowHeight };
            Canvas.SetTop(rowCanvas, RowTop + RowPitch * i);

            // Halo radio skin: 12px translucent circle (RadioButtonAssets "up" state).
            var skinBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 1),
                EndPoint = new Windows.Foundation.Point(0, 0),
            };
            skinBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0x66, 0x2D, 0x2D, 0x2D), Offset = 0 });
            skinBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF), Offset = 1 });

            var skin = new Ellipse { Width = 12, Height = 12, Fill = skinBrush };
            Canvas.SetLeft(skin, 108);
            Canvas.SetTop(skin, 4);
            rowCanvas.Children.Add(skin);

            // Selected state: 10px white radial glow.
            var dotBrush = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                GradientOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
            };
            dotBrush.GradientStops.Add(new GradientStop { Color = Colors.White, Offset = 0.49 });
            dotBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0, 0xFF, 0xFF, 0xFF), Offset = 1 });

            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = dotBrush,
                Visibility = Visibility.Collapsed,
            };
            Canvas.SetLeft(dot, 109);
            Canvas.SetTop(dot, 5);
            rowCanvas.Children.Add(dot);

            var label = new TextBlock
            {
                Text = OptionLabels[i],
                FontFamily = (FontFamily)Application.Current.Resources["ClockFont"],
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Opacity = 0.45,
                IsHitTestVisible = false,
            };
            UiHelpers.PlaceAtBaseline(label, 126, RowHeight / 2 + 4);
            rowCanvas.Children.Add(label);

            // Whole row is clickable, like the halo RadioButton hit area.
            var hit = new Rectangle
            {
                Width = 292,
                Height = RowHeight,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };
            Canvas.SetLeft(hit, 100);
            rowCanvas.Children.Add(hit);

            var row = new OptionRow { Mode = mode, Skin = skin, Dot = dot, Label = label };
            hit.PointerPressed += (_, e) =>
            {
                Select(row.Mode);
                e.Handled = true;
            };
            hit.PointerEntered += (_, _) => SetHovered(row);
            hit.PointerExited += (_, _) => SetHovered(null);

            _rows.Add(row);
            OptionsHost.Children.Add(rowCanvas);
        }
    }

    private void LayoutChrome()
    {
        UiHelpers.PlaceAtBaseline(TitleText, 17, 27);
        UiHelpers.PlaceAtBaseline(SubheadText, 17, 42);
        UiHelpers.PlaceAtBaseline(InstructionText, 0, 133);
        UiHelpers.PlaceAtBaseline(CloseButton, 359, 27);

        Canvas.SetLeft(BorderTop, 5);
        Canvas.SetTop(BorderTop, 5);
        BorderTop.Width = 389;

        Canvas.SetLeft(BorderBottom, 5);
        Canvas.SetTop(BorderBottom, 394);
        BorderBottom.Width = 389;

        Canvas.SetLeft(BorderLeft, 5);
        Canvas.SetTop(BorderLeft, 5);
        BorderLeft.Height = 389;

        Canvas.SetLeft(BorderRight, 394);
        Canvas.SetTop(BorderRight, 5);
        BorderRight.Height = 389;
    }

    private void Select(DisplayMode mode)
    {
        SettingsStore.SetMode(mode);
        UpdateRows();
    }

    private void SetHovered(OptionRow? row)
    {
        _hovered = row;
        UpdateRows();
    }

    private void UpdateRows()
    {
        var selected = SettingsStore.Current.Mode;

        foreach (var row in _rows)
        {
            var isSelected = row.Mode == selected;
            var opacity = isSelected ? 0.70 : row == _hovered ? 0.55 : 0.45;

            row.Label.Opacity = opacity;
            row.Skin.Opacity = opacity;
            row.Dot.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
