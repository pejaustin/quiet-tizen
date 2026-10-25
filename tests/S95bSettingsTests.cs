using System;
using System.Globalization;

class S95bSettingsTests
{
    static void Equal(string expected, string actual)
    {
        if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual);
    }
    static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Invalid native state was accepted");
    }
    static void Main()
    {
        // Tint's native midpoint is 15, although its displayed label is zero.
        Equal("16", SettingValues.Step("15", 0, 30, 1, 1));
        Equal("50", SettingValues.Step("50", 0, 50, 1, 1));
        Equal("0", SettingValues.Step("0", 0, 50, 1, -1));
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        Equal("1.5", SettingValues.Step("1", 0, 2, .5, 1));
        Reject(() => SettingValues.Step("NaN", 0, 50, 1, 1));
        Reject(() => SettingValues.Step("51", 0, 50, 1, 1));
        Reject(() => SettingValues.Step("1", 0, 50, 0, 1));
        Reject(() => SettingValues.Step("1", 0, double.PositiveInfinity, 1, 1));
        Reject(() => SettingValues.Step("1", 50, 0, 1, 1));
        // Native modes have large opaque IDs and duplicate display labels.
        var choices = SettingValues.ChoiceValues("{\"655360\":\"FILMMAKER MODE\",\"393224\":\"Dynamic\",\"327705\":\"Standard\"}");
        Equal("393224", SettingValues.StepChoice(choices, "327705", 1));
        Equal("655360", SettingValues.StepChoice(choices, "393224", 1));
        Equal("655360", SettingValues.StepChoice(choices, "655360", 1));
        Reject(() => SettingValues.StepChoice(choices, "0", 1));
        Reject(() => SettingValues.StepChoice(Array.Empty<string>(), "0", 1));
        Console.WriteLine("S95B range, raw-value, locale and native-choice tests passed.");
    }
}
