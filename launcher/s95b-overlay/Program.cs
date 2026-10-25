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
        if (args.Length == 2 && args[0] == "--preview" && int.TryParse(args[1], out int seconds) && seconds >= 1 && seconds <= 300)
        {
            try { RunOverlay(seconds); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        Console.Error.WriteLine("Use --inspect or --preview SECONDS (1–300).");
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunOverlay(int seconds)
    {
        Environment.SetEnvironmentVariable("XDG_RUNTIME_DIR", "/run/user/5001");
        Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", "wayland-0");
        using (var app = new OverlayWindow(seconds)) app.RunPreview();
    }
}
