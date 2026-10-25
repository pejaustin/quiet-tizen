using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

internal static class Tv
{
    [DllImport("libsource-api.so")] internal static extern IntPtr get_source_list();
    [DllImport("libsource-api.so")] internal static extern int connect_source(int source);
    [DllImport("libvconf.so.0")] internal static extern int vconf_get_int(string key, out int value);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemValue(string key, out string name, out string value);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemEnable(string key, out bool enabled);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemVisible(string key, out bool visible);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemDataList(string key, StringBuilder list);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemDataListEnable(string key, string value, out bool enabled);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpGetItemDataListVisible(string key, string value, out bool visible);
    [DllImport("libsettings-api.so")] internal static extern bool CSharpSetItemValue(string key, string value);
    // Newer wrapper documented by Quiet Tizen; absent on the S95B's firmware.
    [DllImport("libsettings-api.so")] private static extern bool CSharpGetItemMinMaxStepValue(string key, out double min, out double max, out double step);
    [DllImport("libcapi-appfw-app-operation-tv.so.0")] internal static extern int app_operation_get_current_app(string operation, out IntPtr app);
    [DllImport("libc.so.6")] private static extern void free(IntPtr pointer);

    internal static readonly string[] PictureKeys = {
        "picturemode", "brightness", "contrast", "sharpness", "color", "tint", "color-tone", "led-hdr"
    };

    internal static void Inspect()
    {
        int result = app_operation_get_current_app("http://tizen.org/appcontrol/operation/main", out IntPtr home);
        try { Console.WriteLine(JsonSerializer.Serialize(new { homeResult = result, home = Marshal.PtrToStringAnsi(home) })); }
        finally { if (home != IntPtr.Zero) free(home); }
        Console.WriteLine(Marshal.PtrToStringAnsi(get_source_list()));
        foreach (string key in PictureKeys)
        {
            bool ok = CSharpGetItemValue(key, out string name, out string value);
            var list = new StringBuilder(16384);
            bool listOk = CSharpGetItemDataList(key, list);
            double min = 0, max = 0, step = 0;
            bool rangeOk = false;
            try { rangeOk = CSharpGetItemMinMaxStepValue(key, out min, out max, out step); }
            catch (EntryPointNotFoundException) { } // Tizen 6.5 lacks this Tizen 9 entry point.
            Console.WriteLine(JsonSerializer.Serialize(new { key, ok, name, value, listOk, list = list.ToString(), rangeOk, min, max, step }));
        }
    }
}
