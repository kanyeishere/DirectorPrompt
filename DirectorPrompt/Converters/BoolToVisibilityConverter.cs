using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DirectorPrompt.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b       && b;
        var invert    = parameter is string s && s == "Invert";

        return boolValue ^ invert ?
                   Visibility.Visible :
                   Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = value is Visibility v ?
                             v :
                             Visibility.Collapsed;
        var invert = parameter is string s && s == "Invert";

        return (visibility == Visibility.Visible) ^ invert;
    }
}
