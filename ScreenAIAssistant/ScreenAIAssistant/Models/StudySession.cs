using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreenAIAssistant.Models;

/// <summary>
/// Thực thể Phiên học tập (Bảng StudySessions) lưu trữ thống kê giờ học:
/// Gồm Ngày học, Giờ bắt đầu, Giờ kết thúc và Tổng thời lượng.
/// </summary>
public class StudySession
{
    public int Id { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới Môn học (Subject).
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Tên môn học tại thời điểm học.
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày và Giờ bắt đầu phiên học.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Ngày và Giờ kết thúc phiên học.
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Tổng thời gian học tính bằng giây.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Ghi chú phiên học (VD: Tài liệu hoặc nội dung đã học).
    /// </summary>
    public string SessionNote { get; set; } = string.Empty;

    // Navigation property
    public Subject? Subject { get; set; }

    // Thuộc tính hiển thị tiện ích cho UI Binding
    public string DateText => StartTime.ToString("dd/MM/yyyy");
    public string StartTimeText => StartTime.ToString("HH:mm:ss (dd/MM/yyyy)");
    public string EndTimeText => EndTime.ToString("HH:mm:ss (dd/MM/yyyy)");
    public string ShortTimeRangeText => $"{StartTime:dd/MM/yyyy} • {StartTime:HH:mm:ss} → {EndTime:HH:mm:ss}";

    public string DurationFormatted
    {
        get
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, DurationSeconds));
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours} giờ {ts.Minutes:D2} phút {ts.Seconds:D2} giây";
            }
            if (ts.TotalMinutes >= 1)
            {
                return $"{ts.Minutes} phút {ts.Seconds:D2} giây";
            }
            return $"{ts.Seconds} giây";
        }
    }
}

/// <summary>
/// Mô hình một cột trong Biểu đồ thời gian tự học qua từng ngày (7 ngày gần nhất).
/// </summary>
public class DailyStudyBarItem
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string DateSubLabel { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }
    public string TimeValueText { get; set; } = "0p";
    public double BarHeight { get; set; } = 8;
    public string BarColorHex { get; set; } = "#3B82F6";
    public bool IsToday { get; set; }
    public string TooltipText { get; set; } = string.Empty;

    /// <summary>
    /// Tạo dữ liệu biểu đồ cột cho 7 ngày gần nhất từ danh sách các phiên học.
    /// </summary>
    public static List<DailyStudyBarItem> BuildLast7DaysChart(IEnumerable<StudySession> sessions, double maxBarPixelHeight = 115)
    {
        var sessionList = sessions.ToList();
        var today = DateTime.Now.Date;
        var days = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(-6 + offset))
            .ToList();

        var dayTotals = days.Select(d =>
        {
            var daySessions = sessionList.Where(s => s.StartTime.Date == d).ToList();
            int sec = daySessions.Sum(s => Math.Max(0, s.DurationSeconds));
            return (Date: d, Seconds: sec, Count: daySessions.Count);
        }).ToList();

        int maxSeconds = Math.Max(60, dayTotals.Max(x => x.Seconds));
        var result = new List<DailyStudyBarItem>();

        foreach (var item in dayTotals)
        {
            bool isToday = item.Date == today;
            string dayOfWeek = item.Date.DayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                _ => "CN"
            };

            string valueLabel;
            if (item.Seconds <= 0)
            {
                valueLabel = "0p";
            }
            else if (item.Seconds < 60)
            {
                valueLabel = $"{item.Seconds}s";
            }
            else if (item.Seconds < 3600)
            {
                valueLabel = $"{Math.Round(item.Seconds / 60.0, 1):0.#}p";
            }
            else
            {
                valueLabel = $"{Math.Round(item.Seconds / 3600.0, 1):0.#}h";
            }

            double ratio = item.Seconds > 0 ? (double)item.Seconds / maxSeconds : 0;
            double barHeight = item.Seconds > 0
                ? Math.Max(18, Math.Round(ratio * maxBarPixelHeight))
                : 6;

            string barColor = item.Seconds > 0
                ? (isToday ? "#10B981" : "#3B82F6")
                : "#475569";

            var ts = TimeSpan.FromSeconds(item.Seconds);
            string tooltip = item.Seconds > 0
                ? $"Ngày {item.Date:dd/MM/yyyy}: Tổng {ts.Hours}h {ts.Minutes}p {ts.Seconds}s ({item.Count} phiên học)"
                : $"Ngày {item.Date:dd/MM/yyyy}: Chưa có phiên tự học";

            result.Add(new DailyStudyBarItem
            {
                Date = item.Date,
                DayLabel = isToday ? "H.Nay" : dayOfWeek,
                DateSubLabel = item.Date.ToString("dd/MM"),
                TotalSeconds = item.Seconds,
                TimeValueText = valueLabel,
                BarHeight = barHeight,
                BarColorHex = barColor,
                IsToday = isToday,
                TooltipText = tooltip
            });
        }

        return result;
    }
}

/// <summary>
/// Lưu trữ tọa độ (CanvasX, CanvasY) và màu sắc của các thẻ Flashcard / Note ghim trên Bảng Blueprint.
/// </summary>
public class PinLayoutMetadata
{
    public double X { get; set; }
    public double Y { get; set; }
    public string ColorHex { get; set; } = "#FEF9C3";
}

/// <summary>
/// Lớp đại diện cho một Thẻ nhớ Flashcard được GHIM (Note Pinned) trên Bảng Blueprint Canvas.
/// Người dùng có thể kéo thả (Drag) tự do khắp bảng Blueprint và lật xem mặt trước / mặt sau.
/// </summary>
public partial class PinnedFlashcardItem : ObservableObject
{
    private static readonly string LayoutFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenAIAssistant",
        "blueprint_pins_layout.json");

    public Flashcard FlashcardEntity { get; set; } = new();

    public int Id => FlashcardEntity.Id;
    public int SubjectId => FlashcardEntity.SubjectId;
    public int? DocumentId => FlashcardEntity.DocumentId;

    [ObservableProperty]
    private string _question = string.Empty;

    [ObservableProperty]
    private string _answer = string.Empty;

    [ObservableProperty]
    private MasteryStatus _masteryStatus = MasteryStatus.New;

    /// <summary>Tọa độ ngang (Canvas.Left) trên Bảng Blueprint.</summary>
    [ObservableProperty]
    private double _canvasX = 780;

    /// <summary>Tọa độ dọc (Canvas.Top) trên Bảng Blueprint.</summary>
    [ObservableProperty]
    private double _canvasY = 48;

    /// <summary>Màu nền giấy Note ghim (Vàng Post-it, Xanh Mint, Xanh Lam, Hồng Phấn).</summary>
    [ObservableProperty]
    private string _notePaperColorHex = "#FEF9C3";

    [ObservableProperty]
    private bool _isAnswerVisible = true;

    public string PinBadgeText => MasteryStatus switch
    {
        MasteryStatus.Mastered => "📌 Đã thuộc",
        MasteryStatus.Learning => "📌 Đang ôn",
        _ => "📌 Note ghim"
    };

    public string PinThumbColor => MasteryStatus switch
    {
        MasteryStatus.Mastered => "#10B981",
        MasteryStatus.Learning => "#2563EB",
        _ => "#E11D48"
    };

    public string FlipButtonText => IsAnswerVisible ? "🙈 Ẩn mặt sau" : "👁️ Lật mặt sau";

    partial void OnMasteryStatusChanged(MasteryStatus value)
    {
        FlashcardEntity.MasteryStatus = value;
        OnPropertyChanged(nameof(PinBadgeText));
        OnPropertyChanged(nameof(PinThumbColor));
    }

    partial void OnIsAnswerVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(FlipButtonText));
    }

    public void NotifyPositionSaved()
    {
        SavePinLayout(Id, CanvasX, CanvasY, NotePaperColorHex);
    }

    public static PinnedFlashcardItem FromEntity(Flashcard card, int index = 0)
    {
        var layouts = LoadAllPinLayouts();
        double defaultX = 770 + ((index % 2) * 285);
        double defaultY = 44 + ((index / 2) * 225);
        string defaultColor = (index % 4) switch
        {
            0 => "#FEF9C3", // Vàng Post-it
            1 => "#DCFCE7", // Xanh Mint
            2 => "#DBEAFE", // Xanh Lam nhạt
            _ => "#FCE7F3"  // Hồng Phấn
        };

        if (layouts.TryGetValue(card.Id, out var saved))
        {
            defaultX = saved.X;
            defaultY = saved.Y;
            if (!string.IsNullOrWhiteSpace(saved.ColorHex))
            {
                defaultColor = saved.ColorHex;
            }
        }

        return new PinnedFlashcardItem
        {
            FlashcardEntity = card,
            Question = card.Question,
            Answer = card.Answer,
            MasteryStatus = card.MasteryStatus,
            CanvasX = defaultX,
            CanvasY = defaultY,
            NotePaperColorHex = defaultColor,
            IsAnswerVisible = true
        };
    }

    public static void SavePinLayout(int cardId, double x, double y, string colorHex)
    {
        try
        {
            var dict = LoadAllPinLayouts();
            dict[cardId] = new PinLayoutMetadata
            {
                X = Math.Round(x, 1),
                Y = Math.Round(y, 1),
                ColorHex = colorHex
            };

            string? dir = Path.GetDirectoryName(LayoutFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(LayoutFilePath, JsonSerializer.Serialize(dict));
        }
        catch
        {
            // Bỏ qua lỗi I/O phụ trợ
        }
    }

    private static Dictionary<int, PinLayoutMetadata> LoadAllPinLayouts()
    {
        try
        {
            if (File.Exists(LayoutFilePath))
            {
                string json = File.ReadAllText(LayoutFilePath);
                return JsonSerializer.Deserialize<Dictionary<int, PinLayoutMetadata>>(json) ?? new();
            }
        }
        catch
        {
            // Bỏ qua nếu file chưa tồn tại hoặc lỗi định dạng
        }
        return new Dictionary<int, PinLayoutMetadata>();
    }
}

/// <summary>
/// Lớp đại diện cho một mục trong Lịch sử hỏi Trợ lý AI của môn học.
/// </summary>
public class AiHistoryItem
{
    public SnippetNote NoteEntity { get; set; } = new();

    public int Id => NoteEntity.Id;
    public string Title => NoteEntity.Title;
    public string QuestionText => NoteEntity.OcrText;
    public string AiAnswerText => NoteEntity.AiResponse;
    public string ModeName => string.IsNullOrWhiteSpace(NoteEntity.PersonalNote) ? "Trợ lý AI" : NoteEntity.PersonalNote;
    public string CreatedAtText => NoteEntity.CreatedAt.ToString("HH:mm • dd/MM/yyyy");
}
