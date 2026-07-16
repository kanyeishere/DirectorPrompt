using System.Globalization;
using Avalonia.Data.Converters;

namespace DirectorPrompt.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is true;
        var invert    = parameter is "Invert";

        return boolValue ^ invert;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = parameter is "Invert";
        return value is true ^ invert;
    }
}
