using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel Tab 1 — TỔNG QUAN.
/// Hiển thị 3 chỉ số: Giờ học (biểu đồ tự học qua từng ngày), Số môn học, Số deadline cần làm hôm nay.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStudyRepository _repository;

    public ObservableCollection<DailyStudyBarItem> DailyStudyChart { get; } = new();
    public ObservableCollection<StudySession> RecentStudySessions { get; } = new();

    /// <summary>Tổng số giờ học tuần này (tính từ các phiên học trong bảng StudySessions).</summary>
    [ObservableProperty]
    private double _studyHoursThisWeek;

    /// <summary>Tổng thời lượng học hiển thị dạng chuỗi dễ đọc.</summary>
    [ObservableProperty]
    private string _studyTimeSummaryText = "Tổng thời gian tập trung trong 7 ngày qua";

    /// <summary>Tổng số môn học đang quản lý.</summary>
    [ObservableProperty]
    private int _totalSubjects;

    /// <summary>Số deadline đến hạn hôm nay.</summary>
    [ObservableProperty]
    private int _todayDeadlinesCount;

    [ObservableProperty]
    private bool _hasStudySessions;

    [ObservableProperty]
    private string _greetingText = string.Empty;

    [ObservableProperty]
    private string _todayText = string.Empty;

    public DashboardViewModel(IStudyRepository repository)
    {
        _repository = repository;
        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        int hour = DateTime.Now.Hour;
        GreetingText = hour switch
        {
            < 11 => "☀️ Chào buổi sáng! Sẵn sàng học chưa?",
            < 14 => "🌤️ Chào buổi trưa! Nghỉ ngơi rồi học tiếp nhé.",
            < 18 => "🌇 Chào buổi chiều! Giữ vững nhịp học nào.",
            _ => "🌙 Chào buổi tối! Ôn bài nhẹ nhàng thôi nhé."
        };
        TodayText = DateTime.Now.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
    }

    /// <summary>
    /// Tải số liệu thống kê tổng quan &amp; Biểu đồ thời gian tự học qua từng ngày từ SQLite.
    /// </summary>
    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            UpdateGreeting();

            TotalSubjects = await _repository.GetTotalSubjectsCountAsync();
            StudyHoursThisWeek = await _repository.GetTotalStudyHoursThisWeekAsync();

            var sessions = await _repository.GetStudySessionsAsync();

            // Dựng biểu đồ cột 7 ngày gần nhất
            DailyStudyChart.Clear();
            foreach (var bar in DailyStudyBarItem.BuildLast7DaysChart(sessions, 130))
            {
                DailyStudyChart.Add(bar);
            }

            RecentStudySessions.Clear();
            foreach (var s in sessions.Take(5))
            {
                RecentStudySessions.Add(s);
            }

            HasStudySessions = RecentStudySessions.Count > 0;
            int totalSeconds = sessions.Sum(s => Math.Max(0, s.DurationSeconds));
            var ts = TimeSpan.FromSeconds(totalSeconds);
            StudyTimeSummaryText = totalSeconds > 0
                ? $"Đã ghi nhận {sessions.Count} phiên học ({(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s)"
                : "Tổng thời gian tập trung trong 7 ngày qua";

            TodayDeadlinesCount = 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải dữ liệu tổng quan: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
