using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

internal static class Program
{
    static int Main(string[] args)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) => {
            string path = "/usr/share/dotnet.tizen/framework/" + name.Name + ".dll";
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        if (args.Length == 1 && args[0] == "--inspect")
        {
            Tv.Inspect();
            return 0;
        }
        if (args.Length == 6 && args[0] == "--restore" && int.TryParse(args[4], out int source))
        {
            try { Tv.Restore(args[1], args[2], args[3], source, args[5]); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
        if (args.Length == 2 && (args[0] == "--preview" || args[0] == "--controls") && int.TryParse(args[1], out int seconds) && seconds >= 1 && seconds <= 300)
        {
            try { RunOverlay(seconds, args[0] == "--controls"); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        try { RunRegistered(args); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunOverlay(int seconds, bool interactive)
    {
        SetPreviewEnvironment();
        using (var app = new OverlayWindow(seconds, interactive)) app.RunPreview();
    }

    static void SetPreviewEnvironment()
    {
        Environment.SetEnvironmentVariable("XDG_RUNTIME_DIR", "/run");
        Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", "wayland-0");
        Environment.SetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS", "kernel:path=/sys/fs/kdbus/5001-user/bus");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunRegistered(string[] args)
    {
        // Registered application launch uses the vendor-provided environment
        // and lifecycle. The host is not required for rendering or navigation.
        using (var app = new OverlayWindow(90, true)) app.Run(args.Length == 0 ? new[] { "quiet-s95b" } : args);
    }
}
