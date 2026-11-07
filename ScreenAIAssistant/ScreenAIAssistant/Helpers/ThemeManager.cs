using System.Windows;

namespace ScreenAIAssistant.Helpers;

/// <summary>
/// Quản lý chuyển đổi giao diện Sáng / Tối bằng cách thay ResourceDictionary màu (vị trí 0) trong App.Resources.
/// Các Style dùng DynamicResource nên toàn bộ giao diện đổi màu tức thì.
/// </summary>
public static class ThemeManager
{
    private const string LightThemeUri = "pack://application:,,,/ScreenAIAssistant;component/Resources/Colors.xaml";
    private const string DarkThemeUri = "pack://application:,,,/ScreenAIAssistant;component/Resources/ColorsDark.xaml";

    public static bool IsDarkTheme { get; private set; }

    public static void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        var merged = app.Resources.MergedDictionaries;
        var newColors = new ResourceDictionary
        {
            Source = new Uri(isDark ? DarkThemeUri : LightThemeUri, UriKind.Absolute)
        };

        if (merged.Count > 0)
        {
            merged[0] = newColors;
        }
        else
        {
            merged.Add(newColors);
        }

        IsDarkTheme = isDark;
    }
}
