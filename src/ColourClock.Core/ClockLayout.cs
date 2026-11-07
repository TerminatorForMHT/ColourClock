namespace TheColourClock.Core;

/// <summary>
/// Geometry lifted from the original SWF (all values in Flash stage units = DIPs at 1x).
/// The stage is 960x600 in the SWF metadata.
/// </summary>
public static class ClockLayout
{
    public const double DesignWidth = 960;
    public const double DesignHeight = 600;

    // Top-left brand block (sprites 16/15 + text records).
    public const double TitleLeft = 17;
    public const double TitleTop = 17;
    public const double SubheadLeft = 17;
    public const double SubheadTop = 42;
    public const double SmallTextFontSize = 12;

    // Clock holder (sprites 8/9).
    public const double ClockFontSize = 190.5;      // 127px * 1.5 sprite scale
    public const double ClockTextWidth = 855.9;     // 570.6px * 1.5
    public const double ClockTextLeft = -427.95;    // -283.3 * 1.5
    public const double SingleTextTop = -119.925;   // text box top inside the holder
    public const double BothClockTop = -194.925;    // y = -75px + scaled box top
    public const double BothColourTop = 3.075;      // y = +123px + scaled box top
    public const double TextBoxHeight = 136;        // unscaled text field height (px)

    // Divider (shape 6 inside sprite 7).
    public const double DividerWidth = 734;
    public const double SingleDividerTop = 75;
    public const double BothDividerTop = 0;

    // Responsive rules from MainTimeline.checkPos().
    public const double HolderScaleBreakpoint = 810;
    public const double SubheadHideBreakpoint = 400;

    // Colours / alphas from the original graphics calls.
    public const double TitleOpacity = 0.70;
    public const double SubheadOpacity = 0.45;

    /// <summary>
    /// Ascent ratio of the original font (Avenir LT 65 Medium: DefineFont3 ascent 17921 / em 20480).
    /// Used to convert the SWF text-box top into a baseline position.
    /// </summary>
    public const double AscentRatio = 0.875;

    /// <summary>Holder scale factor for a stage width (MainTimeline.checkPos).</summary>
    public static double GetHolderScale(double width)
        => width <= HolderScaleBreakpoint ? width / HolderScaleBreakpoint : 1.0;

    /// <summary>Baseline for a text box whose top edge is <paramref name="boxTop"/>.</summary>
    public static double BaselineAt(double boxTop, double fontSize)
        => boxTop + fontSize * AscentRatio;
}
