namespace ColourClock.App;

public enum LaunchMode
{
    Clock,
    Settings,
    Screensaver,
    Preview
}

/// <summary>
/// Parses the classic Windows screen saver command line:
/// /s (full screen), /c[:hwnd] (config), /p hwnd (preview), no args = desktop clock.
/// </summary>
public sealed class LaunchOptions
{
    public LaunchMode Mode { get; private init; } = LaunchMode.Clock;

    public IntPtr ParentHandle { get; private init; }

    public static LaunchOptions Current { get; set; } = new();

    public static LaunchOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new LaunchOptions { Mode = LaunchMode.Clock };
        }

        var first = args[0].Trim();
        if (first.Length < 2)
        {
            return new LaunchOptions();
        }

        // Accept "/s", "-s", "s" (the shell may drop the slash when the value is a quoted path).
        var token = first.TrimStart('/', '-').ToLowerInvariant();
        var separator = token.IndexOf(':');
        var command = separator >= 0 ? token[..separator] : token;
        var argument = separator >= 0 ? token[(separator + 1)..] : (args.Length > 1 ? args[1] : null);

        switch (command)
        {
            case "s":
                return new LaunchOptions { Mode = LaunchMode.Screensaver };

            case "c":
                return new LaunchOptions { Mode = LaunchMode.Settings };

            case "p":
            {
                var handle = ParseHandle(argument);
                if (handle != IntPtr.Zero)
                {
                    return new LaunchOptions { Mode = LaunchMode.Preview, ParentHandle = handle };
                }

                return new LaunchOptions();
            }

            case "clock":
                return new LaunchOptions { Mode = LaunchMode.Clock };

            default:
                return new LaunchOptions();
        }
    }

    private static IntPtr ParseHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return IntPtr.Zero;
        }

        if (long.TryParse(value.Trim(), out var raw) && raw != 0)
        {
            return new IntPtr(raw);
        }

        return IntPtr.Zero;
    }
}
