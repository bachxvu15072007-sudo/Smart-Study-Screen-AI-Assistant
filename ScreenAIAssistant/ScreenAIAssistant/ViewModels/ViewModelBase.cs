using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// Lớp cơ sở chuẩn cho toàn bộ ViewModels trong dự án ScreenAIAssistant.
/// Cung cấp cờ IsBusy, ErrorMessage, StatusMessage phục vụ Data Binding và chống đơ UI.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = string.Empty;

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>
    /// Xóa thông báo lỗi hiện tại.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = string.Empty;
    }
}
