using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace VibeCoders.Converters;

/// <summary>
/// Converts a boolean value to <see cref="Visibility"/>.
/// Pass <c>ConverterParameter="Invert"</c> to invert the logic
/// (true becomes Collapsed, false becomes Visible).
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
