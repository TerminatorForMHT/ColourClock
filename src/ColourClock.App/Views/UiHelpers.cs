using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using TheColourClock.Core;
using Windows.Foundation;

namespace ColourClock.App.Views;

internal static class UiHelpers
{
    /// <summary>
    /// Distance from the top of a TextBlock to its first baseline. Falls back to the
    /// original font's ascent ratio when layout metrics are not available yet.
    /// </summary>
    public static double GetBaselineOffset(TextBlock block)
    {
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var offset = block.BaselineOffset;
        if (double.IsNaN(offset) || offset <= 0)
        {
            offset = block.FontSize * ClockLayout.AscentRatio;
        }

        return offset;
    }

    /// <summary>Positions a TextBlock so that its first baseline sits at <paramref name="baseline"/>.</summary>
    public static void PlaceAtBaseline(TextBlock block, double left, double baseline)
    {
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, baseline - GetBaselineOffset(block));
    }

    public static double MeasureWidth(TextBlock block)
    {
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = block.DesiredSize.Width;
        return double.IsNaN(width) ? 0 : width;
    }

    /// <summary>TweenMax-style tween used for the url underline.</summary>
    public static void Animate(DependencyObject target, string property, double to, int milliseconds, EasingMode ease)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds)),
            EasingFunction = new QuadraticEase { EasingMode = ease },
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}
