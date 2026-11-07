using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenAIAssistant.Helpers;

/// <summary>
/// So sánh chuỗi binding với ConverterParameter → bool.
/// Dùng cho RadioButton điều hướng (Sidebar, Tab trong Không gian Học tập).
/// </summary>
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter?.ToString() ?? string.Empty : Binding.DoNothing;
    }
}

/// <summary>
/// So sánh chuỗi binding với ConverterParameter → Visibility (Visible / Collapsed).
/// Dùng để hiển thị đúng nội dung của Tab đang chọn.
/// </summary>
public class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
