using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

// Windows only lists screen savers that live in %SystemRoot%\System32.  Putting the
// real executable there is not an option: WinUI resolves ms-appx:///Assets/... against
// the executable directory, so the app must stay in Program Files.  This tiny launcher
// is what System32 contains; it forwards /s, /c, /p <hwnd> and /a to the real app.
internal static class Program
{
    private const string KeyPath = @"SOFTWARE\The Colour Clock";
    private const string ExeName = "TheColourClock.exe";

    [STAThread]
    private static void Main(string[] args)
    {
        var exe = ResolveExe();
        if (exe == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exe),
                Arguments = JoinArguments(args),
            });
        }
        catch
        {
            // The screen saver simply does nothing if the app cannot be started.
        }
    }

    private static string ResolveExe()
    {
        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            try
            {
                using (var key = root.OpenSubKey(KeyPath))
                {
                    var dir = key == null ? null : key.GetValue("InstallPath") as string;
                    if (string.IsNullOrEmpty(dir))
                    {
                        continue;
                    }

                    var candidate = Path.Combine(dir, ExeName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // Fall through to the next hive.
            }
        }

        return null;
    }

    private static string JoinArguments(string[] args)
    {
        var builder = new StringBuilder();
        foreach (var arg in args)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            if (arg.IndexOf(' ') < 0 && arg.IndexOf('\t') < 0)
            {
                builder.Append(arg);
            }
            else
            {
                builder.Append('"').Append(arg.Replace("\"", "\\\"")).Append('"');
            }
        }

        return builder.ToString();
    }
}
