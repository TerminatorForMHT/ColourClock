namespace TheColourClock.Core;

/// <summary>An 8-bit RGB color.</summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public string ToHexUpper() => $"#{R:X2}{G:X2}{B:X2}";

    public override string ToString() => ToHexUpper();
}
