using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.Helpers;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;
/// <summary>
/// Phần logic: Bắt đầu / Kết thúc học tập và biểu đồ thời gian tự học.
/// </summary>
public partial class SubjectWorkspaceViewModel
{
    #region Logic 1: Bắt đầu / Kết thúc Học tập & Biểu đồ Thời gian Tự học Qua Từng Ngày

    private async Task LoadStudySessionsAsync()
    {
        if (ActiveSubject == null) return;

        var sessions = await _repository.GetStudySessionsAsync(ActiveSubject.Id);

        // Cập nhật biểu đồ cột 7 ngày gần nhất của môn học
        SubjectDailyStudyChart.Clear();
        foreach (var bar in DailyStudyBarItem.BuildLast7DaysChart(sessions, 82))
        {
            SubjectDailyStudyChart.Add(bar);
        }

        SubjectStudySessions.Clear();
        foreach (var s in sessions)
        {
            SubjectStudySessions.Add(s);
        }

        HasSubjectStudySessions = SubjectStudySessions.Count > 0;
        int totalSec = sessions.Sum(s => Math.Max(0, s.DurationSeconds));
        var ts = TimeSpan.FromSeconds(totalSec);

        if (totalSec <= 0)
        {
            SubjectTotalStudyTimeText = "Tổng giờ học môn này: 0 phút";
        }
        else if (ts.TotalHours >= 1)
        {
            SubjectTotalStudyTimeText = $"Tổng giờ học: {(int)ts.TotalHours}h {ts.Minutes:D2}p {ts.Seconds:D2}s ({sessions.Count} phiên)";
        }
        else
        {
            SubjectTotalStudyTimeText = $"Tổng giờ học: {ts.Minutes}p {ts.Seconds:D2}s ({sessions.Count} phiên)";
        }

        if (!IsStudying)
        {
            var latest = sessions.FirstOrDefault();
            SessionTimeInfoText = latest != null
                ? $"Phiên gần nhất: {latest.StartTime:dd/MM/yyyy} • Bắt đầu {latest.StartTime:HH:mm:ss} → Kết thúc {latest.EndTime:HH:mm:ss} ({latest.DurationFormatted})"
                : "Bấm 'Bắt đầu học tập' để ghi nhận ngày giờ bắt đầu & kết thúc";
        }
    }

    [RelayCommand]
    public void StartStudySession()
    {
        if (ActiveSubject == null || IsStudying) return;

        ClearError();
        _currentSessionStartTime = DateTime.Now;
        LiveTimerText = "00:00:00";
        IsStudying = true;
        _studyTimer.Start();

        SessionTimeInfoText = $"🟢 Đang học từ: {_currentSessionStartTime:HH:mm:ss} — Ngày {_currentSessionStartTime:dd/MM/yyyy}";
        StatusMessage = $"Đã bắt đầu phiên học lúc {_currentSessionStartTime:HH:mm:ss dd/MM/yyyy}.";
    }

    [RelayCommand]
    public async Task StopStudySessionAsync()
    {
        if (ActiveSubject == null || !IsStudying) return;

        try
        {
            _studyTimer.Stop();
            IsStudying = false;

            DateTime endTime = DateTime.Now;
            int durationSeconds = Math.Max(1, (int)Math.Round((endTime - _currentSessionStartTime).TotalSeconds));

            string docTitle = SelectedDocument?.Title ?? "Tự học trên Bảng Blueprint";
            var newSession = new StudySession
            {
                SubjectId = ActiveSubject.Id,
                SubjectName = ActiveSubject.Name,
                StartTime = _currentSessionStartTime,
                EndTime = endTime,
                DurationSeconds = durationSeconds,
                SessionNote = docTitle
            };

            await _repository.AddStudySessionAsync(newSession);
            await LoadStudySessionsAsync();

            StatusMessage = $"Đã lưu vào biểu đồ giờ học: {_currentSessionStartTime:dd/MM/yyyy} ({_currentSessionStartTime:HH:mm:ss} → {endTime:HH:mm:ss}) • {newSession.DurationFormatted}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể lưu phiên học: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task DeleteStudySessionAsync(StudySession? session)
    {
        if (session == null) return;

        try
        {
            await _repository.DeleteStudySessionAsync(session.Id);
            await LoadStudySessionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xóa phiên học: {ex.Message}";
        }
    }

    #endregion
}
