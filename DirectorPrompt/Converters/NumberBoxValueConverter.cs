using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DirectorPrompt.Converters;

public sealed class NumberBoxValueConverter : IValueConverter
{
    public static readonly NumberBoxValueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? null : System.Convert.ToDouble(value, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return BindingOperations.DoNothing;

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return System.Convert.ChangeType(value, type, culture);
    }
}
