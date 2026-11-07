using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel Tab 3 — LỊCH, DEADLINE & TO-DO LIST.
/// (Khung giao diện — logic thêm deadline / to-do sẽ được bổ sung sau.)
/// </summary>
public partial class CalendarViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedDateText = string.Empty;

    public CalendarViewModel()
    {
        UpdateSelectedDateText();
    }

    partial void OnSelectedDateChanged(DateTime value) => UpdateSelectedDateText();

    private void UpdateSelectedDateText()
    {
        SelectedDateText = SelectedDate.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
    }
}
