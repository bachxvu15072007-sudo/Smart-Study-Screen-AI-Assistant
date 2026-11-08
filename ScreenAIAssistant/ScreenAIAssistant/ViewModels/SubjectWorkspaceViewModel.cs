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
/// ViewModel KHÔNG GIAN HỌC TẬP của một Môn học.
/// Bao gồm:
/// 1. Chế độ Bắt đầu / Kết thúc Học tập + Biểu đồ cột thời gian tự học qua từng ngày.
/// 2. Tab "📄 Tài Liệu &amp; Bảng Blueprint": Mặt bảng Blueprint chứa tài liệu và các Flashcard / Note Pinned kéo thả tự do.
/// 3. Cửa sổ Popup tạo &amp; ghim Flashcard (điền Mặt trước &amp; Mặt sau).
/// 4. Tab "🤖 Trợ Lý AI": Đặt câu hỏi và lưu trữ toàn bộ Lịch sử hỏi AI của môn học.
/// </summary>
public partial class SubjectWorkspaceViewModel : ViewModelBase
{
    private readonly IStudyRepository _repository;
    private readonly DispatcherTimer _studyTimer;
    private List<SubjectDocumentItem> _allDocumentsCache = new();
    private List<PinnedFlashcardItem> _allPinnedCardsCache = new();
    private List<AiHistoryItem> _allAiHistoryCache = new();
    private int _currentPageIndex;
    private DateTime _currentSessionStartTime;

    /// <summary>Sự kiện yêu cầu quay lại danh sách Môn học.</summary>
    public event Func<Task>? CloseRequested;

    [ObservableProperty]
    private Subject? _activeSubject;

    /// <summary>Tab đang chọn trên thanh taskbar của môn học: "Documents" | "AI".</summary>
    [ObservableProperty]
    private string _selectedTab = "Documents";

    #region 1. Chế độ Bắt đầu Học tập & Biểu đồ Thời gian Tự học Qua Từng Ngày

    public ObservableCollection<DailyStudyBarItem> SubjectDailyStudyChart { get; } = new();
    public ObservableCollection<StudySession> SubjectStudySessions { get; } = new();

    [ObservableProperty]
    private bool _isStudying;

    [ObservableProperty]
    private string _liveTimerText = "00:00:00";

    [ObservableProperty]
    private string _sessionTimeInfoText = "Bấm 'Bắt đầu học tập' để ghi nhận ngày giờ bắt đầu & kết thúc";

    [ObservableProperty]
    private string _subjectTotalStudyTimeText = "Tổng giờ học môn này: 0 phút";

    [ObservableProperty]
    private bool _hasSubjectStudySessions;

    #endregion

    #region 2. Tab Tài liệu & Bảng Blueprint (Kèm Note Pinned Flashcard Kéo Thả Tự Do)

    public ObservableCollection<SubjectDocumentItem> SubjectDocuments { get; } = new();
    public ObservableCollection<DocumentPageSection> SelectedDocumentPages { get; } = new();
    public ObservableCollection<PinnedFlashcardItem> PinnedFlashcards { get; } = new();

    [ObservableProperty]
    private SubjectDocumentItem? _selectedDocument;

    [ObservableProperty]
    private bool _hasDocuments;

    [ObservableProperty]
    private string _documentSearchKeyword = string.Empty;

    [ObservableProperty]
    private string _selectedFormatFilter = "ALL";

    [ObservableProperty]
    private DocumentPageSection? _currentCanvasPage;

    [ObservableProperty]
    private string _pageIndicatorText = "0 / 0";

    [ObservableProperty]
    private double _canvasZoom = 1.0;

    [ObservableProperty]
    private string _canvasZoomPercentText = "100%";

    /// <summary>Chiều cao động của mặt Bảng Blueprint để người dùng thoải mái đọc tài liệu và kéo thả Note ghim.</summary>
    [ObservableProperty]
    private double _boardCanvasHeight = 1040;

    /// <summary>Trạng thái đóng/mở cửa sổ Popup tạo Flashcard ghim lên bảng Blueprint.</summary>
    [ObservableProperty]
    private bool _isPinPopupOpen;

    /// <summary>Tương thích bí danh với thuộc tính cũ.</summary>
    public bool IsPinFormOpen
    {
        get => IsPinPopupOpen;
        set => IsPinPopupOpen = value;
    }

    [ObservableProperty]
    private string _newPinQuestion = string.Empty;

    [ObservableProperty]
    private string _newPinAnswer = string.Empty;

    [ObservableProperty]
    private string _selectedNoteColorHex = "#FEF9C3";

    [ObservableProperty]
    private bool _hasPinnedFlashcards;

    [ObservableProperty]
    private int _pinnedFlashcardsCount;

    #endregion

    #region 3. Tab Trợ Lý AI & Lưu Trữ Lịch Sử Hỏi AI

    public ObservableCollection<AiHistoryItem> AiHistoryItems { get; } = new();

    public List<string> AiModes { get; } = new()
    {
        "Giải thích chi tiết",
        "Tóm tắt ý chính",
        "Dịch thuật học thuật",
        "Tạo câu hỏi ôn tập",
        "Hỏi đáp tự do"
    };

    [ObservableProperty]
    private string _selectedAiMode = "Giải thích chi tiết";

    [ObservableProperty]
    private string _aiPromptInput = string.Empty;

    [ObservableProperty]
    private string _aiSearchKeyword = string.Empty;

    [ObservableProperty]
    private AiHistoryItem? _selectedAiHistory;

    [ObservableProperty]
    private string _selectedAiQuestionDetail = string.Empty;

    [ObservableProperty]
    private string _selectedAiAnswerDetail = string.Empty;

    [ObservableProperty]
    private bool _hasAiHistory;

    #endregion

    public SubjectWorkspaceViewModel(IStudyRepository repository)
    {
        _repository = repository;
        _studyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _studyTimer.Tick += OnStudyTimerTick;
    }

    private void OnStudyTimerTick(object? sender, EventArgs e)
    {
        if (!IsStudying) return;
        var elapsed = DateTime.Now - _currentSessionStartTime;
        LiveTimerText = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    /// <summary>
    /// Nạp Không gian Học tập cho môn học (Tài liệu, Flashcard ghim trên Blueprint, Lịch sử hỏi AI, Biểu đồ giờ học).
    /// </summary>
    public async Task LoadSubjectAsync(Subject subject)
    {
        try
        {
            IsBusy = true;
            ClearError();
            StatusMessage = string.Empty;
            IsPinPopupOpen = false;

            ActiveSubject = subject;
            SelectedTab = "Documents";
            SelectedFormatFilter = "ALL";
            DocumentSearchKeyword = string.Empty;
            AiSearchKeyword = string.Empty;
            ResetCanvasZoom();

            // 1. Nạp danh sách Tài liệu của môn
            var docs = await _repository.GetSubjectDocumentsAsync(subject.Id);
            _allDocumentsCache = docs.Select(d => new SubjectDocumentItem { DocumentEntity = d }).ToList();

            SelectedDocument = null;
            ApplyDocumentFilter();

            // 2. Nạp danh sách Flashcard được ghim (Note Pinned) trên Bảng Blueprint của môn
            await LoadPinnedFlashcardsAsync();

            // 3. Nạp Lịch sử hỏi AI của môn
            await LoadAiHistoryAsync();

            // 4. Nạp Biểu đồ thời gian tự học qua từng ngày & Lịch sử phiên học của môn
            await LoadStudySessionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở Không gian Học tập: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (IsStudying)
        {
            await StopStudySessionAsync();
        }

        if (CloseRequested != null)
        {
            await CloseRequested.Invoke();
        }
    }

}
