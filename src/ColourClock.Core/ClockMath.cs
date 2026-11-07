namespace TheColourClock.Core;

public enum DisplayMode
{
    Clock = 1,
    Colour = 2,
    Both = 3
}

/// <summary>
/// The stage colour rotates smoothly through the colour wheel: one full spectrum
/// cycle every <see cref="ColourCycleSeconds"/> seconds (96 cycles per day), so the
/// hue never wraps with a visible jump.  The hexadecimal read-out is derived from
/// the very same colour, so the code always matches what is on screen.
/// </summary>
public static class ClockMath
{
    private const int ColourCycleSeconds = 900;
    private const double Saturation = 0.55;
    private const double Lightness = 0.45;

    /// <summary>Returns the RGB colour of the given instant.</summary>
    public static RgbColor GetColor(DateTime time)
    {
        var seconds = time.Hour * 3600 + time.Minute * 60 + time.Second;
        var hue = seconds % ColourCycleSeconds * (360.0 / ColourCycleSeconds);
        return HslToRgb(hue, Saturation, Lightness);
    }

    /// <summary>The six hexadecimal digits, e.g. "3F0A9C".</summary>
    public static string GetColourText(DateTime time)
    {
        var c = GetColor(time);
        return $"{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    /// <summary>The clock text, e.g. "14:03:27" (24 hour, zero padded).</summary>
    public static string GetClockText(DateTime time)
        => $"{time.Hour:00}:{time.Minute:00}:{time.Second:00}";

    public static DisplayTexts GetDisplayTexts(DisplayMode mode, DateTime time)
        => mode switch
        {
            DisplayMode.Clock => new DisplayTexts(GetClockText(time), null),
            DisplayMode.Colour => new DisplayTexts(GetColourText(time), null),
            _ => new DisplayTexts(GetClockText(time), GetColourText(time)),
        };

    /// <summary>Standard HSL to RGB conversion with a fully continuous hue wheel.</summary>
    private static RgbColor HslToRgb(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var sector = hue / 60.0;
        var second = chroma * (1 - Math.Abs(sector % 2 - 1));

        double r, g, b;
        switch ((int)Math.Floor(sector) % 6)
        {
            case 0: r = chroma; g = second; b = 0; break;
            case 1: r = second; g = chroma; b = 0; break;
            case 2: r = 0; g = chroma; b = second; break;
            case 3: r = 0; g = second; b = chroma; break;
            case 4: r = second; g = 0; b = chroma; break;
            default: r = chroma; g = 0; b = second; break;
        }

        var match = lightness - chroma / 2;
        return new RgbColor(
            ToByte(r + match),
            ToByte(g + match),
            ToByte(b + match));
    }

    private static byte ToByte(double value)
        => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}

public readonly record struct DisplayTexts(string Primary, string? Secondary);
