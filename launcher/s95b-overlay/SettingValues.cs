using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

internal static class SettingValues
{
    internal static string Step(string raw, double min, double max, double step, int direction)
    {
        if (direction != -1 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction));
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double current)
            || !Finite(current) || !Finite(min) || !Finite(max) || !Finite(step)
            || min > max || step <= 0 || current < min || current > max)
            throw new InvalidOperationException("Invalid native slider range or current value");
        double value = Math.Max(min, Math.Min(max, current + direction * step));
        if (!Finite(value)) throw new InvalidOperationException("Invalid slider result");
        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    internal static List<string> ChoiceValues(string json)
    {
        var values = new List<string>();
        if (json == null) return values;
        using (var document = JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Unexpected native choice list");
            foreach (var entry in document.RootElement.EnumerateObject())
                if (entry.Value.ValueKind == JsonValueKind.String) values.Add(entry.Name);
        }
        values.Sort((a, b) => {
            if (long.TryParse(a, out long x) && long.TryParse(b, out long y)) return x.CompareTo(y);
            return StringComparer.Ordinal.Compare(a, b);
        });
        return values;
    }

    internal static string StepChoice(IReadOnlyList<string> allowed, string current, int direction)
    {
        if (direction != -1 && direction != 1) throw new ArgumentOutOfRangeException(nameof(direction));
        int index = -1;
        for (int i = 0; i < allowed.Count; i++) if (allowed[i] == current) index = i;
        if (index < 0) throw new InvalidOperationException("Current value is outside the available choices");
        return allowed[Math.Max(0, Math.Min(allowed.Count - 1, index + direction))];
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
