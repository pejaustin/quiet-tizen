// PLACEHOLDER: prototype visual treatment and helper wording, not approved by Austin.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Tizen.Applications;
using System.Text.Json;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

internal sealed class OverlayWindow : NUIApplication
{
    private sealed class Row
    {
        internal string Title, Key;
        internal int Source;
        internal bool Advanced;
        internal TextLabel Label;
    }
    private readonly List<Row> rows = new List<Row>();
    private readonly int seconds;
    private readonly bool interactive;
    private View highlight;
    private TextLabel status;
    private Timer timeout, refresh;
    private int selected;
    private Action quitPreview;
    private string pendingKey, pendingValue, lastKey;
    private int pendingSource;
    private long pendingAt, lastKeyAt;
    private bool previewMode;
    private const int X = 1440, Y = 170, Width = 440, RowHeight = 46;

    internal OverlayWindow(int seconds, bool interactive) : base("", WindowMode.Transparent, WindowType.Notification)
    {
        this.seconds = seconds;
        this.interactive = interactive;
        quitPreview = Exit;
    }

    // An unregistered diagnostic has no ApplicationInfo. The stock 6.5
    // NUICoreBackend.Run dereferences it before creating a window. For a bounded
    // preview only, initialize the shipped NUI Application directly. A deployed
    // app must use normal package registration and lifecycle instead.
    internal void RunPreview()
    {
        previewMode = true;
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
            refresh?.Stop();
            refresh?.Dispose();
            initialized.RemoveEventHandler(nativeApp, handler);
            ((IDisposable)nativeApp).Dispose();
            Console.WriteLine("overlay_closed");
        }
    }

    private void PreviewInitialized(object sender, object args) => OnCreate();

    protected override void OnAppControlReceived(AppControlReceivedEventArgs args)
    {
        base.OnAppControlReceived(args);
        if (args.ReceivedAppControl.ExtraData.TryGet("launch_type", out string launchType) && launchType == "preload")
        {
            Exit();
            return;
        }
        if (status != null)
        {
            foreach (var row in rows) row.Label.Text = RowText(row);
            GetDefaultWindow().Raise();
            timeout.Stop(); timeout.Start();
        }
    }

    protected override void OnTerminate()
    {
        timeout?.Stop(); timeout?.Dispose();
        refresh?.Stop(); refresh?.Dispose();
        WriteState("closed");
        base.OnTerminate();
    }

    private void WriteState(string state)
    {
        if (previewMode) return;
        try { File.WriteAllText("/tmp/quiet-s95b-state.json", JsonSerializer.Serialize(new { state, pid = Process.GetCurrentProcess().Id, selected, interactive })); }
        catch (Exception error) { Console.Error.WriteLine(error.Message); }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        try
        {
            LoadRows();
            var window = GetDefaultWindow();
            window.Title = "quiet-tizen-s95b-overlay";
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
            for (int i = 0; i < rows.Count; i++) rows[i].Label = Text(RowText(rows[i]), X + 24, Y + 72 + i * RowHeight, 22);
            status = Text(interactive ? "OK: input · ←→: picture · Back: close" : "Preview · ↑↓ move · Back closes", X + 24, Y + height - 44, 16);
            window.KeyEvent += OnKey;
            window.Raise();
            timeout = new Timer((uint)seconds * 1000);
            timeout.Tick += (sender, args) => { quitPreview(); return false; };
            timeout.Start();
            refresh = new Timer(200);
            refresh.Tick += (sender, args) => { CheckPending(); return true; };
            refresh.Start();
            Console.WriteLine("overlay_ready seconds=" + seconds + " interactive=" + interactive);
            WriteState("ready");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            quitPreview();
        }
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
                rows.Add(new Row { Source = type, Title = "HDMI " + (type - 12) + (name.Length == 0 ? "" : "  " + name) });
            }
        }
        string[] names = { "Picture mode", "Brightness", "Contrast", "Sharpness", "Color", "Tint", "Color tone", "Peak brightness" };
        for (int i = 0; i < Tv.PictureKeys.Length; i++)
        {
            string key = Tv.PictureKeys[i];
            if (!Tv.CSharpGetItemVisible(key, out bool visible) || !visible) continue;
            if (Tv.CSharpGetItemValue(key, out string label, out string value)) rows.Add(new Row { Key = key, Title = names[i] });
        }
        rows.Add(new Row { Advanced = true, Title = "Advanced settings" });
    }

    private void OnKey(object sender, Tizen.NUI.Window.KeyEventArgs args)
    {
        if (args.Key.State != Key.StateType.Down) return;
        string key = args.Key.KeyPressedName;
        Console.WriteLine("key=" + key);
        if (!previewMode) { timeout.Stop(); timeout.Start(); }
        if (key == "Escape" || key == "XF86Back" || key == "Back" || key == "BackSpace" || key == "XF86Exit") { quitPreview(); return; }
        long now = Stopwatch.GetTimestamp();
        if (key == lastKey && (now - lastKeyAt) / (double)Stopwatch.Frequency < .12) return;
        lastKey = key; lastKeyAt = now;
        if (key == "Up") selected = Math.Max(0, selected - 1);
        if (key == "Down") selected = Math.Min(rows.Count - 1, selected + 1);
        highlight.Position2D = new Position2D(X + 12, Y + 68 + selected * RowHeight);
        WriteState("key:" + key);
        if (!interactive || rows.Count == 0 || pendingSource != 0 || pendingKey != null) return;
        try
        {
            Row row = rows[selected];
            if (row.Advanced && (key == "Return" || key == "KP_Enter"))
            {
                Tv.OpenAdvancedSettings();
                quitPreview();
                return;
            }
            if (row.Source != 0 && (key == "Return" || key == "KP_Enter"))
            {
                int result = Tv.connect_source(row.Source);
                WriteState("source_request:" + row.Source + ":" + result);
                // Match upstream's API convention: only negative results are errors.
                if (result < 0) throw new InvalidOperationException("Input request failed: " + result);
                Tv.OpenViewer();
                pendingSource = row.Source;
                pendingAt = Stopwatch.GetTimestamp();
                status.Text = "Switching input…";
                Console.WriteLine("source_requested=" + row.Source);
            }
            if (row.Key != null && (key == "Left" || key == "Right"))
            {
                pendingValue = Tv.Adjust(row.Key, key == "Left" ? -1 : 1);
                pendingKey = row.Key;
                pendingAt = Stopwatch.GetTimestamp();
                status.Text = "Updating…";
            }
        }
        catch (Exception error) { status.Text = error.Message; Console.Error.WriteLine(error); }
    }

    private string RowText(Row row)
    {
        if (row.Advanced) return row.Title;
        if (row.Source != 0)
        {
            Tv.vconf_get_int("memory/system/source_type", out int current);
            return (current == row.Source ? "● " : "   ") + row.Title;
        }
        bool readable = Tv.CSharpGetItemValue(row.Key, out string name, out string value);
        bool enabled = Tv.CSharpGetItemEnable(row.Key, out bool available) && available;
        return row.Title + "   " + (readable ? name : "—") + (enabled ? "" : "  ·");
    }

    private void CheckPending()
    {
        if (pendingSource == 0 && pendingKey == null) return;
        try
        {
            bool done = pendingSource != 0
                ? Tv.vconf_get_int("memory/system/source_type", out int source) == 0 && source == pendingSource
                : Tv.CSharpGetItemValue(pendingKey, out string name, out string value) && value == pendingValue;
            bool expired = (Stopwatch.GetTimestamp() - pendingAt) / (double)Stopwatch.Frequency > 3;
            if (!done && !expired) return;
            Console.WriteLine(JsonSerializer.Serialize(new { verified = done, setting = pendingKey, source = pendingSource, requested = pendingValue }));
            status.Text = done ? "OK: input · ←→: picture · Back: close" : "Change not confirmed; check TV";
            pendingSource = 0; pendingKey = null; pendingValue = null;
            foreach (var row in rows) row.Label.Text = RowText(row);
        }
        catch (Exception error)
        {
            pendingSource = 0; pendingKey = null; pendingValue = null;
            status.Text = "Could not verify change";
            Console.Error.WriteLine(error);
        }
    }

    private View Panel(int x, int y, int w, int h, Color color)
    {
        var view = new View { Position2D = new Position2D(x, y), Size2D = new Size2D(w, h), BackgroundColor = color };
        GetDefaultWindow().Add(view);
        return view;
    }

    private TextLabel Text(string text, int x, int y, float size)
    {
        var label = new TextLabel {
            Text = text, Position2D = new Position2D(x, y), Size2D = new Size2D(Width - 48, 40),
            PixelSize = size, TextColor = new Color(.96f, .96f, .96f, 1), VerticalAlignment = VerticalAlignment.Center
        };
        GetDefaultWindow().Add(label);
        return label;
    }
}
