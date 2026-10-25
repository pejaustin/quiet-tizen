// PLACEHOLDER: prototype visual treatment and helper wording, not approved by Austin.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.Json;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

internal sealed class OverlayWindow : NUIApplication
{
    private readonly List<string> rows = new List<string>();
    private readonly int seconds;
    private View highlight;
    private Timer timeout;
    private int selected;
    private Action quitPreview;
    private const int X = 1440, Y = 170, Width = 440, RowHeight = 46;

    internal OverlayWindow(int seconds) : base("", WindowMode.Transparent, WindowType.Notification)
    {
        this.seconds = seconds;
    }

    // An unregistered diagnostic has no ApplicationInfo. The stock 6.5
    // NUICoreBackend.Run dereferences it before creating a window. For a bounded
    // preview only, initialize the shipped NUI Application directly. A deployed
    // app must use normal package registration and lifecycle instead.
    internal void RunPreview()
    {
        Type type = typeof(NUIApplication).Assembly.GetType("Tizen.NUI.Application", true);
        MethodInfo factory = type.GetMethod("NewApplication", new[] { typeof(string), typeof(WindowMode), typeof(WindowType) });
        object nativeApp = factory.Invoke(null, new object[] { "", WindowMode.Transparent, WindowType.Notification });
        quitPreview = () => type.GetMethod("Quit").Invoke(nativeApp, null);
        EventInfo initialized = type.GetEvent("Initialized");
        Delegate handler = Delegate.CreateDelegate(initialized.EventHandlerType, this,
            GetType().GetMethod(nameof(PreviewInitialized), BindingFlags.NonPublic | BindingFlags.Instance));
        initialized.AddEventHandler(nativeApp, handler);
        try { type.GetMethod("MainLoop").Invoke(nativeApp, null); }
        finally
        {
            timeout?.Stop();
            timeout?.Dispose();
            initialized.RemoveEventHandler(nativeApp, handler);
            ((IDisposable)nativeApp).Dispose();
            Console.WriteLine("overlay_closed");
        }
    }

    private void PreviewInitialized(object sender, object args) => OnCreate();

    protected override void OnCreate()
    {
        base.OnCreate();
        try
        {
            LoadRows();
            var window = GetDefaultWindow();
            window.Title = "s95b-overlay-preview";
            window.BackgroundColor = new Color(0, 0, 0, 0);
            window.WindowSize = new Size2D(1920, 1080);
            if (!window.SetNotificationLevel(NotificationLevel.Top))
                throw new InvalidOperationException("Notification plane rejected");
            window.SetAcceptFocus(true);
            int height = 135 + rows.Count * RowHeight;
            window.SetInputRegion(new Rectangle(X, Y, Width, height));
            Panel(X, Y, Width, height, new Color(.075f, .082f, .098f, .95f));
            Text("Inputs & picture", X + 24, Y + 18, 28);
            highlight = Panel(X + 12, Y + 68, Width - 24, RowHeight, new Color(.24f, .26f, .29f, 1));
            for (int i = 0; i < rows.Count; i++) Text(rows[i], X + 24, Y + 72 + i * RowHeight, 22);
            Text("Preview · ↑↓ move · Back closes", X + 24, Y + height - 44, 18);
            window.KeyEvent += OnKey;
            window.Raise();
            timeout = new Timer((uint)seconds * 1000);
            timeout.Tick += (sender, args) => { quitPreview(); return false; };
            timeout.Start();
            Console.WriteLine("overlay_ready seconds=" + seconds);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            quitPreview();
        }
    }

    protected override void OnTerminate()
    {
        timeout?.Stop();
        timeout?.Dispose();
        Console.WriteLine("overlay_closed");
        base.OnTerminate();
    }

    private void LoadRows()
    {
        Tv.vconf_get_int("memory/system/source_type", out int current);
        using (var sources = JsonDocument.Parse(Marshal.PtrToStringAnsi(Tv.get_source_list()) ?? "[]"))
        {
            foreach (var source in sources.RootElement.EnumerateArray())
            {
                int type = source.GetProperty("sourceType").GetInt32();
                if (type < 13 || type > 16) continue;
                string name = Uri.UnescapeDataString(source.GetProperty("editName").GetString() ?? "");
                if (name.Length > 21) name = name.Substring(0, 20) + "…";
                rows.Add((type == current ? "● " : "   ") + "HDMI " + (type - 12) + (name.Length == 0 ? "" : "  " + name));
            }
        }
        string[] names = { "Picture mode", "Brightness", "Contrast", "Sharpness", "Color", "Tint", "Color tone", "Peak brightness" };
        for (int i = 0; i < Tv.PictureKeys.Length; i++)
        {
            string key = Tv.PictureKeys[i];
            if (!Tv.CSharpGetItemVisible(key, out bool visible) || !visible) continue;
            if (Tv.CSharpGetItemValue(key, out string label, out string value)) rows.Add(names[i] + "   " + label);
        }
    }

    private void OnKey(object sender, Tizen.NUI.Window.KeyEventArgs args)
    {
        if (args.Key.State != Key.StateType.Down) return;
        string key = args.Key.KeyPressedName;
        Console.WriteLine("key=" + key);
        if (key == "Escape" || key == "XF86Back" || key == "Back" || key == "BackSpace" || key == "XF86Exit") quitPreview();
        if (key == "Up") selected = Math.Max(0, selected - 1);
        if (key == "Down") selected = Math.Min(rows.Count - 1, selected + 1);
        highlight.Position2D = new Position2D(X + 12, Y + 68 + selected * RowHeight);
    }

    private View Panel(int x, int y, int w, int h, Color color)
    {
        var view = new View { Position2D = new Position2D(x, y), Size2D = new Size2D(w, h), BackgroundColor = color };
        GetDefaultWindow().Add(view);
        return view;
    }

    private void Text(string text, int x, int y, float size)
    {
        var label = new TextLabel {
            Text = text, Position2D = new Position2D(x, y), Size2D = new Size2D(Width - 48, 40),
            PixelSize = size, TextColor = new Color(.96f, .96f, .96f, 1), VerticalAlignment = VerticalAlignment.Center
        };
        GetDefaultWindow().Add(label);
    }
}
