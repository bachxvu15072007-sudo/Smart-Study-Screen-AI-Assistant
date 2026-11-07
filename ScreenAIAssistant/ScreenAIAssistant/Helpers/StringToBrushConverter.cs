using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenAIAssistant.Helpers;

/// <summary>
/// Converter chuyển đổi chuỗi mã màu Hex (VD: "#2563EB") sang SolidColorBrush chuẩn cho WPF Data Binding.
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    private static readonly BrushConverter _brushConverter = new();
    private static readonly SolidColorBrush _defaultBrush = new(Color.FromRgb(37, 99, 235)); // #2563EB

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                if (_brushConverter.ConvertFromString(hex.Trim()) is SolidColorBrush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // Fallback về màu mặc định nếu mã Hex không hợp lệ
            }
        }

        return _defaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color.ToString();
        }
        return "#2563EB";
    }
}
