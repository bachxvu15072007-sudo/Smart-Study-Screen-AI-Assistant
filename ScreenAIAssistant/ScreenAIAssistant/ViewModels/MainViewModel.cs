using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel chính của cửa sổ MainWindow.
/// Điều phối chuyển đổi giữa các màn hình (Dashboard, StudyNotes, Flashcards) và lưu trữ GeminiApiKey.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private string _currentTabName = "Dashboard";

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    public MainViewModel(DashboardViewModel dashboardViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _currentView = _dashboardViewModel;
    }

    /// <summary>
    /// Chuyển sang màn hình Tổng quan & Môn học (DashboardView).
    /// </summary>
    [RelayCommand]
    public async Task NavigateToDashboardAsync()
    {
        CurrentView = _dashboardViewModel;
        CurrentTabName = "Dashboard";
        await _dashboardViewModel.LoadDashboardDataAsync();
    }

    /// <summary>
    /// Chuyển sang màn hình Ghi chú OCR & AI (StudyNotesView - TV1).
    /// </summary>
    [RelayCommand]
    public void NavigateToStudyNotes()
    {
        CurrentTabName = "StudyNotes";
        StatusMessage = "Đang ở phân hệ Ghi chú OCR & AI (Thành viên 1).";
    }

    /// <summary>
    /// Chuyển sang màn hình Thẻ nhớ Flashcard (FlashcardView - TV2).
    /// </summary>
    [RelayCommand]
    public void NavigateToFlashcards()
    {
        CurrentTabName = "Flashcards";
        StatusMessage = "Đang ở phân hệ Thẻ ghi nhớ Flashcard (Thành viên 2).";
    }
}
