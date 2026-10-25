using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class Tv
{
    [DllImport("libsource-api.so")] internal static extern IntPtr get_source_list();
    [DllImport("libsource-api.so")] internal static extern int connect_source(int source);
    [DllImport("libvconf.so.0")] internal static extern int vconf_get_int(string key, out int value);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemValue(string key, out string name, out string value);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemEnable(string key, out bool enabled);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemVisible(string key, out bool visible);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemDataListEnable(string key, string value, out bool enabled);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemDataListVisible(string key, string value, out bool visible);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpSetItemValue(string key, string value);
    // The S95B exports this older C ABI, not the newer CSharp range wrapper.
    [DllImport("libsettings-api.so")] internal static extern bool settings_get_item_minmax_step(string key, out double min, out double max, out double step);
    [DllImport("libsettings-api.so")] private static extern bool settings_get_item_data_list(string key, out IntPtr json);
    [DllImport("libcapi-appfw-app-operation-tv.so.0")] internal static extern int app_operation_get_current_app(string operation, out IntPtr app);
    [DllImport("libc.so.6")] private static extern void free(IntPtr pointer);

    internal static readonly string[] PictureKeys = {
        "picturemode", "brightness", "contrast", "sharpness", "color", "tint", "color-tone", "led-hdr"
    };

    internal static string ChoicesJson(string key)
    {
        IntPtr json = IntPtr.Zero;
        try { return settings_get_item_data_list(key, out json) ? Marshal.PtrToStringAnsi(json) : null; }
        finally { if (json != IntPtr.Zero) free(json); }
    }

    internal static List<string> AllowedChoices(string key)
    {
        var allowed = new List<string>();
        foreach (var value in SettingValues.ChoiceValues(ChoicesJson(key)))
        {
            if (CSharpGetItemDataListEnable(key, value, out bool enabled) && enabled
                && CSharpGetItemDataListVisible(key, value, out bool visible) && visible)
                allowed.Add(value);
        }
        return allowed;
    }

    internal static string Adjust(string key, int direction)
    {
        if (Array.IndexOf(PictureKeys, key) < 0) throw new InvalidOperationException("Unknown picture control");
        if (!CSharpGetItemVisible(key, out bool visible) || !visible
            || !CSharpGetItemEnable(key, out bool enabled) || !enabled)
            throw new InvalidOperationException("Unavailable for the current input");
        if (!CSharpGetItemValue(key, out string name, out string current))
            throw new InvalidOperationException("Could not read current value");
        string next = settings_get_item_minmax_step(key, out double min, out double max, out double step)
            ? SettingValues.Step(current, min, max, step, direction)
            : SettingValues.StepChoice(AllowedChoices(key), current, direction);
        if (next != current && !CSharpSetItemValue(key, next))
            throw new InvalidOperationException("TV rejected the change");
        Console.WriteLine(JsonSerializer.Serialize(new { setting = key, before = current, requested = next }));
        return next;
    }

    internal static void Restore(string key, string expected, string original, int source, string mode)
    {
        if (Array.IndexOf(PictureKeys, key) < 0 || key == "picturemode")
            throw new InvalidOperationException("Restore requires a known non-mode picture setting");
        if (vconf_get_int("memory/system/source_type", out int currentSource) != 0 || currentSource != source
            || !CSharpGetItemValue("picturemode", out string modeName, out string currentMode) || currentMode != mode)
            throw new InvalidOperationException("Source or picture mode changed; restore stopped");
        if (!CSharpGetItemValue(key, out string name, out string current)) throw new InvalidOperationException("Value unavailable");
        if (current == original) { Console.WriteLine("Already restored"); return; }
        if (current != expected) throw new InvalidOperationException("Value changed since the test; restore stopped");
        if (!settings_get_item_minmax_step(key, out double min, out double max, out double step)
            || !double.TryParse(original, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
            throw new InvalidOperationException("Original value is outside the native range");
        if (!CSharpSetItemValue(key, original)) throw new InvalidOperationException("Restore rejected");
        for (int i = 0; i < 10; i++)
        {
            if (CSharpGetItemValue(key, out name, out current) && current == original)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { restored = key, value = current, source, mode }));
                return;
            }
            System.Threading.Thread.Sleep(100);
        }
        throw new InvalidOperationException("Restore readback failed");
    }

    internal static void Inspect()
    {
        int result = app_operation_get_current_app("http://tizen.org/appcontrol/operation/main", out IntPtr home);
        try { Console.WriteLine(JsonSerializer.Serialize(new { homeResult = result, home = Marshal.PtrToStringAnsi(home) })); }
        finally { if (home != IntPtr.Zero) free(home); }
        Console.WriteLine(Marshal.PtrToStringAnsi(get_source_list()));
        foreach (string key in PictureKeys)
        {
            bool ok = CSharpGetItemValue(key, out string name, out string value);
            double min = 0, max = 0, step = 0;
            bool rangeOk = false;
            rangeOk = settings_get_item_minmax_step(key, out min, out max, out step);
            Console.WriteLine(JsonSerializer.Serialize(new { key, ok, name, value, choices = ChoicesJson(key), allowed = AllowedChoices(key), rangeOk, min, max, step }));
        }
    }
}
