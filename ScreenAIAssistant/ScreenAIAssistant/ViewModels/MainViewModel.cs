using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenAIAssistant.Helpers;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel chính của MainWindow.
/// Điều phối 3 tab (Tổng quan • Môn học • Lịch), mở Không gian Học tập, thu gọn Sidebar và đổi theme Sáng/Tối.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly SubjectsViewModel _subjectsViewModel;
    private readonly SubjectWorkspaceViewModel _workspaceViewModel;
    private readonly CalendarViewModel _calendarViewModel;

    [ObservableProperty]
    private ViewModelBase _currentView;

    /// <summary>Tab đang chọn trên Sidebar: "Overview" | "Subjects" | "Calendar".</summary>
    [ObservableProperty]
    private string _currentTabName = "Overview";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _themeIcon = "🌙";

    [ObservableProperty]
    private string _themeLabel = "Chế độ tối";

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        SubjectsViewModel subjectsViewModel,
        SubjectWorkspaceViewModel workspaceViewModel,
        CalendarViewModel calendarViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _subjectsViewModel = subjectsViewModel;
        _workspaceViewModel = workspaceViewModel;
        _calendarViewModel = calendarViewModel;
        _currentView = _dashboardViewModel;

        _subjectsViewModel.OpenWorkspaceRequested += OpenWorkspaceAsync;
        _workspaceViewModel.CloseRequested += NavigateToSubjectsAsync;
    }

    [RelayCommand]
    public async Task NavigateToOverviewAsync()
    {
        CurrentTabName = "Overview";
        CurrentView = _dashboardViewModel;
        await _dashboardViewModel.LoadDashboardDataAsync();
    }

    [RelayCommand]
    public async Task NavigateToSubjectsAsync()
    {
        CurrentTabName = "Subjects";
        CurrentView = _subjectsViewModel;
        await _subjectsViewModel.LoadSubjectsAsync();
    }

    [RelayCommand]
    public void NavigateToCalendar()
    {
        CurrentTabName = "Calendar";
        CurrentView = _calendarViewModel;
    }

    private async Task OpenWorkspaceAsync(Subject subject)
    {
        CurrentTabName = "Subjects";
        CurrentView = _workspaceViewModel;
        await _workspaceViewModel.LoadSubjectAsync(subject);
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeManager.ApplyTheme(IsDarkTheme);
        ThemeIcon = IsDarkTheme ? "☀️" : "🌙";
        ThemeLabel = IsDarkTheme ? "Chế độ sáng" : "Chế độ tối";
    }
}
