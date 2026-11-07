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

    #region Logic 2: Bảng Blueprint Tài liệu & Cửa sổ Popup Ghim Flashcard (Note Pinned Kéo Thả Tự Do)

    partial void OnDocumentSearchKeywordChanged(string value) => ApplyDocumentFilter();

    partial void OnSelectedDocumentChanged(SubjectDocumentItem? value)
    {
        if (value != null)
        {
            _ = LoadDocumentToCanvasAsync(value);
        }
        else
        {
            SelectedDocumentPages.Clear();
            CurrentCanvasPage = null;
            PageIndicatorText = "0 / 0";
            UpdateBoardCanvasHeight();
        }
    }

    private void ApplyDocumentFilter()
    {
        SubjectDocuments.Clear();
        var query = _allDocumentsCache.AsEnumerable();

        if (SelectedFormatFilter != "ALL")
        {
            query = query.Where(d => d.FileType.Equals(SelectedFormatFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(DocumentSearchKeyword))
        {
            string kw = DocumentSearchKeyword.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(kw) || d.FileName.ToLower().Contains(kw));
        }

        foreach (var doc in query)
        {
            SubjectDocuments.Add(doc);
        }

        HasDocuments = _allDocumentsCache.Count > 0;

        if (SelectedDocument == null || !SubjectDocuments.Contains(SelectedDocument))
        {
            SelectedDocument = SubjectDocuments.FirstOrDefault();
        }
    }

    private async Task LoadDocumentToCanvasAsync(SubjectDocumentItem docItem)
    {
        try
        {
            var pages = await SubjectDocumentHelper.ReadDocumentPagesAsync(
                docItem.DocumentEntity.FilePath,
                docItem.DocumentEntity.ExtractedContent);

            SelectedDocumentPages.Clear();
            foreach (var p in pages)
            {
                SelectedDocumentPages.Add(p);
            }

            _currentPageIndex = 0;
            UpdateCurrentCanvasPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở tài liệu lên Bảng Blueprint: {ex.Message}";
        }
    }

    private void UpdateCurrentCanvasPage()
    {
        if (SelectedDocumentPages.Count == 0)
        {
            CurrentCanvasPage = null;
            PageIndicatorText = "0 / 0";
            UpdateBoardCanvasHeight();
            return;
        }

        _currentPageIndex = Math.Clamp(_currentPageIndex, 0, SelectedDocumentPages.Count - 1);
        CurrentCanvasPage = SelectedDocumentPages[_currentPageIndex];
        PageIndicatorText = $"{_currentPageIndex + 1} / {SelectedDocumentPages.Count}";
        _ = SubjectDocumentHelper.EnsurePageRenderedAsync(CurrentCanvasPage);
        UpdateBoardCanvasHeight();
    }

    private void UpdateBoardCanvasHeight()
    {
        double docHeight = (CurrentCanvasPage?.CanvasHeight ?? 900) + 120;
        double maxPinY = PinnedFlashcards.Count > 0 ? PinnedFlashcards.Max(p => p.CanvasY) + 260 : 900;
        BoardCanvasHeight = Math.Max(1040, Math.Max(docHeight, maxPinY));
    }

    [RelayCommand]
    public void FilterDocumentFormat(string? format)
    {
        SelectedFormatFilter = string.IsNullOrWhiteSpace(format) ? "ALL" : format;
        ApplyDocumentFilter();
    }

    [RelayCommand]
    public async Task ImportDocumentFileAsync()
    {
        if (ActiveSubject == null) return;

        var dialog = new OpenFileDialog
        {
            Title = $"Thêm tài liệu cho môn {ActiveSubject.Name}",
            Filter = "Tài liệu học tập (*.pdf;*.docx;*.doc;*.pptx;*.ppt)|*.pdf;*.docx;*.doc;*.pptx;*.ppt|" +
                     "PDF (*.pdf)|*.pdf|Word (*.docx;*.doc)|*.docx;*.doc|PowerPoint (*.pptx;*.ppt)|*.pptx;*.ppt",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            ClearError();
            SubjectDocumentItem? lastAdded = null;

            foreach (string filePath in dialog.FileNames)
            {
                var (docEntity, _) = await SubjectDocumentHelper.ImportDocumentForSubjectAsync(filePath, ActiveSubject.Id);
                var saved = await _repository.AddSubjectDocumentAsync(docEntity);
                lastAdded = new SubjectDocumentItem { DocumentEntity = saved };
                _allDocumentsCache.Insert(0, lastAdded);
            }

            SelectedFormatFilter = "ALL";
            DocumentSearchKeyword = string.Empty;
            ApplyDocumentFilter();

            if (lastAdded != null)
            {
                SelectedDocument = lastAdded;
            }

            StatusMessage = $"Đã mở {dialog.FileNames.Length} tài liệu lên Bảng Blueprint.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi thêm tài liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSelectedDocumentAsync()
    {
        if (SelectedDocument == null) return;

        string title = SelectedDocument.Title;

        if (Application.Current?.Dispatcher.CheckAccess() == true &&
            Application.Current?.MainWindow is { IsLoaded: true })
        {
            var confirm = MessageBox.Show(
                $"Xóa tài liệu \"{title}\" khỏi môn học?",
                "Xác nhận xóa tài liệu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;
        }

        try
        {
            IsBusy = true;
            await _repository.DeleteSubjectDocumentAsync(SelectedDocument.Id);
            _allDocumentsCache.Remove(SelectedDocument);
            SelectedDocument = null;
            ApplyDocumentFilter();
            StatusMessage = $"Đã xóa tài liệu \"{title}\".";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xóa tài liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenDocumentExternal()
    {
        if (SelectedDocument == null) return;

        try
        {
            SubjectDocumentHelper.OpenWithDefaultProgram(SelectedDocument.FilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở file: {ex.Message}";
        }
    }

    [RelayCommand]
    public void PreviousCanvasPage()
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdateCurrentCanvasPage();
        }
    }

    [RelayCommand]
    public void NextCanvasPage()
    {
        if (_currentPageIndex < SelectedDocumentPages.Count - 1)
        {
            _currentPageIndex++;
            UpdateCurrentCanvasPage();
        }
    }

    [RelayCommand]
    public void ZoomInCanvas()
    {
        CanvasZoom = Math.Min(2.50, Math.Round(CanvasZoom + 0.10, 2));
        CanvasZoomPercentText = $"{Math.Round(CanvasZoom * 100)}%";
    }

    [RelayCommand]
    public void ZoomOutCanvas()
    {
        CanvasZoom = Math.Max(0.45, Math.Round(CanvasZoom - 0.10, 2));
        CanvasZoomPercentText = $"{Math.Round(CanvasZoom * 100)}%";
    }

    [RelayCommand]
    public void ResetCanvasZoom()
    {
        CanvasZoom = 1.0;
        CanvasZoomPercentText = "100%";
    }

    /// <summary>
    /// Nạp danh sách Flashcard được ghim (Note Pinned) trên Bảng Blueprint của môn học.
    /// </summary>
    private async Task LoadPinnedFlashcardsAsync()
    {
        if (ActiveSubject == null) return;

        var cards = await _repository.GetFlashcardsAsync(ActiveSubject.Id);
        _allPinnedCardsCache = cards.Select((c, idx) => PinnedFlashcardItem.FromEntity(c, idx)).ToList();

        PinnedFlashcards.Clear();
        foreach (var item in _allPinnedCardsCache)
        {
            PinnedFlashcards.Add(item);
        }

        PinnedFlashcardsCount = PinnedFlashcards.Count;
        HasPinnedFlashcards = PinnedFlashcardsCount > 0;
        UpdateBoardCanvasHeight();
    }

    /// <summary>
    /// Mở cửa sổ Popup điền Mặt trước &amp; Mặt sau để tạo Flashcard ghim lên Bảng Blueprint.
    /// </summary>
    [RelayCommand]
    public void TogglePinForm()
    {
        ClearError();
        IsPinPopupOpen = !IsPinPopupOpen;
        if (IsPinPopupOpen && string.IsNullOrWhiteSpace(NewPinQuestion) && SelectedDocumentPages.Count > 0)
        {
            NewPinQuestion = $"[Trang {_currentPageIndex + 1}] ";
        }
    }

    [RelayCommand]
    public void ClosePinPopup()
    {
        ClearError();
        IsPinPopupOpen = false;
    }

    [RelayCommand]
    public void SelectNoteColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            SelectedNoteColorHex = colorHex;
        }
    }

    /// <summary>
    /// Tạo và ghim một thẻ nhớ Flashcard (Note Pinned) mới lên Bảng Blueprint, có thể kéo thả khắp bảng.
    /// </summary>
    [RelayCommand]
    public async Task PinFlashcardToCanvasAsync()
    {
        if (ActiveSubject == null) return;

        ClearError();
        if (string.IsNullOrWhiteSpace(NewPinQuestion))
        {
            ErrorMessage = "Vui lòng nhập Mặt trước (Câu hỏi / Tiêu đề Note) trước khi ghim lên Bảng Blueprint!";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPinAnswer))
        {
            ErrorMessage = "Vui lòng nhập Mặt sau (Đáp án / Nội dung lưu ý) của thẻ Flashcard!";
            return;
        }

        try
        {
            IsBusy = true;
            string questionText = NewPinQuestion.Trim();

            var newCard = new Flashcard
            {
                SubjectId = ActiveSubject.Id,
                Question = questionText,
                Answer = NewPinAnswer.Trim(),
                MasteryStatus = MasteryStatus.New,
                LastReviewedAt = DateTime.Now
            };

            var saved = await _repository.AddFlashcardAsync(newCard);

            // Đặt vị trí khởi tạo kế bên phải tài liệu trên Bảng Blueprint để người dùng kéo đi tùy ý
            int idx = PinnedFlashcards.Count;
            double initX = 770 + ((idx % 2) * 285);
            double initY = 48 + ((idx / 2) * 225);

            PinnedFlashcardItem.SavePinLayout(saved.Id, initX, initY, SelectedNoteColorHex);
            var pinnedItem = PinnedFlashcardItem.FromEntity(saved, idx);

            _allPinnedCardsCache.Add(pinnedItem);
            PinnedFlashcards.Add(pinnedItem);
            PinnedFlashcardsCount = PinnedFlashcards.Count;
            HasPinnedFlashcards = PinnedFlashcardsCount > 0;
            UpdateBoardCanvasHeight();

            NewPinQuestion = string.Empty;
            NewPinAnswer = string.Empty;
            IsPinPopupOpen = false;
            StatusMessage = "📌 Đã ghim Flashcard lên Bảng Blueprint! Bạn có thể nắm chuột kéo thẻ đi khắp bảng.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể ghim Flashcard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void FlipPinnedCard(PinnedFlashcardItem? item)
    {
        if (item == null) return;
        item.IsAnswerVisible = !item.IsAnswerVisible;
    }

    [RelayCommand]
    public async Task CyclePinnedCardMasteryAsync(PinnedFlashcardItem? item)
    {
        if (item == null) return;

        try
        {
            var nextStatus = item.MasteryStatus switch
            {
                MasteryStatus.New => MasteryStatus.Learning,
                MasteryStatus.Learning => MasteryStatus.Mastered,
                _ => MasteryStatus.New
            };

            await _repository.UpdateMasteryStatusAsync(item.Id, nextStatus);
            item.MasteryStatus = nextStatus;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật trạng thái thẻ ghim: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task UnpinFlashcardAsync(PinnedFlashcardItem? item)
    {
        if (item == null) return;

        if (Application.Current?.Dispatcher.CheckAccess() == true &&
            Application.Current?.MainWindow is { IsLoaded: true })
        {
            var confirm = MessageBox.Show(
                $"Gỡ ghim và xóa thẻ Note \"{item.Question}\" khỏi Bảng Blueprint?",
                "Xác nhận gỡ thẻ ghim",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;
        }

        try
        {
            await _repository.DeleteFlashcardAsync(item.Id);
            _allPinnedCardsCache.Remove(item);
            PinnedFlashcards.Remove(item);
            PinnedFlashcardsCount = PinnedFlashcards.Count;
            HasPinnedFlashcards = PinnedFlashcardsCount > 0;
            UpdateBoardCanvasHeight();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể gỡ ghim Flashcard: {ex.Message}";
        }
    }

    #endregion

    #region Logic 3: Tab Trợ Lý AI & Lưu Trữ Lịch Sử Hỏi AI

    partial void OnAiSearchKeywordChanged(string value) => ApplyAiHistoryFilter();

    partial void OnSelectedAiHistoryChanged(AiHistoryItem? value)
    {
        if (value != null)
        {
            SelectedAiQuestionDetail = value.QuestionText;
            SelectedAiAnswerDetail = value.AiAnswerText;
        }
        else
        {
            SelectedAiQuestionDetail = string.Empty;
            SelectedAiAnswerDetail = string.Empty;
        }
    }

    private async Task LoadAiHistoryAsync()
    {
        if (ActiveSubject == null) return;

        var notes = await _repository.GetNotesAsync(ActiveSubject.Id);
        _allAiHistoryCache = notes.Select(n => new AiHistoryItem { NoteEntity = n }).ToList();
        ApplyAiHistoryFilter();
    }

    private void ApplyAiHistoryFilter()
    {
        AiHistoryItems.Clear();
        var query = _allAiHistoryCache.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(AiSearchKeyword))
        {
            string kw = AiSearchKeyword.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(kw) ||
                x.QuestionText.ToLower().Contains(kw) ||
                x.AiAnswerText.ToLower().Contains(kw));
        }

        foreach (var item in query)
        {
            AiHistoryItems.Add(item);
        }

        HasAiHistory = _allAiHistoryCache.Count > 0;
        if (SelectedAiHistory == null || !AiHistoryItems.Contains(SelectedAiHistory))
        {
            SelectedAiHistory = AiHistoryItems.FirstOrDefault();
        }
    }

    [RelayCommand]
    public void UseCurrentDocumentPageForAi()
    {
        if (CurrentCanvasPage == null)
        {
            ErrorMessage = "Chưa có trang tài liệu nào đang mở trên Bảng Blueprint để lấy nội dung.";
            return;
        }

        ClearError();
        if (!string.IsNullOrWhiteSpace(CurrentCanvasPage.Content))
        {
            AiPromptInput = CurrentCanvasPage.Content.Trim();
        }
        else
        {
            AiPromptInput = $"Hãy giải thích các khái niệm trọng tâm trong tài liệu \"{SelectedDocument?.Title}\" ({CurrentCanvasPage.PageLabel}).";
        }
    }

    [RelayCommand]
    public async Task AskAiAndSaveHistoryAsync()
    {
        if (ActiveSubject == null) return;

        ClearError();
        if (string.IsNullOrWhiteSpace(AiPromptInput))
        {
            ErrorMessage = "Vui lòng nhập câu hỏi hoặc nội dung cần hỏi Trợ lý AI!";
            return;
        }

        try
        {
            IsBusy = true;
            string prompt = AiPromptInput.Trim();
            string shortTitle = prompt.Length > 60 ? prompt[..60] + "..." : prompt;

            string generatedResponse = BuildSmartStudyAiResponse(SelectedAiMode, prompt, ActiveSubject.Name, SelectedDocument?.Title);

            var noteEntity = new SnippetNote
            {
                SubjectId = ActiveSubject.Id,
                Title = shortTitle,
                OcrText = prompt,
                AiResponse = generatedResponse,
                PersonalNote = SelectedAiMode,
                IsFavorite = false,
                CreatedAt = DateTime.Now
            };

            var saved = await _repository.AddNoteAsync(noteEntity);
            var historyItem = new AiHistoryItem { NoteEntity = saved };

            _allAiHistoryCache.Insert(0, historyItem);
            AiSearchKeyword = string.Empty;
            ApplyAiHistoryFilter();
            SelectedAiHistory = historyItem;
            AiPromptInput = string.Empty;

            StatusMessage = "Đã lưu câu hỏi & phản hồi vào Lịch sử hỏi AI của môn học.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xử lý câu hỏi AI: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildSmartStudyAiResponse(string mode, string prompt, string subjectName, string? docTitle)
    {
        string docContext = string.IsNullOrWhiteSpace(docTitle) ? $"Môn học: {subjectName}" : $"Môn: {subjectName} • Tài liệu: {docTitle}";
        return mode switch
        {
            "Tóm tắt ý chính" =>
                $"📌 TÓM TẮT TRỌNG TÂM ({docContext})\n" +
                $"• Chủ đề yêu cầu: {prompt}\n" +
                $"• Ý chính 1: Nắm vững định nghĩa cốt lõi, vai trò và phạm vi áp dụng của nội dung trên trong môn {subjectName}.\n" +
                $"• Ý chính 2: Phân biệt rõ các thành phần cấu trúc, quy trình hoạt động và mối liên hệ giữa các tầng/bước.\n" +
                $"• Lưu ý ôn tập: Bạn có thể bấm nút '📌 Ghim thành Flashcard lên Blueprint' để đính lưu ý này ngay lên bảng học tập.",

            "Dịch thuật học thuật" =>
                $"🌐 BẢN DỊCH & THUẬT NGỮ HỌC THUẬT ({docContext})\n" +
                $"• Văn bản gốc: \"{prompt}\"\n" +
                $"• Giải nghĩa chuyên ngành: Nội dung trên trình bày các khái niệm chuyên môn trong {subjectName}. Khi dịch sang tiếng Việt chuyên ngành, cần giữ nguyên các thuật ngữ chuẩn quốc tế trong ngoặc đơn để dễ đối chiếu tài liệu gốc.",

            "Tạo câu hỏi ôn tập" =>
                $"🎯 CÂU HỎI TỰ KIỂM TRA KIẾN THỨC ({docContext})\n" +
                $"• Câu hỏi: Dựa trên nội dung \"{prompt}\", đặc điểm quan trọng nhất cần nhớ là gì?\n" +
                $"• Gợi ý trả lời: Trình bày khái niệm, nguyên lý hoạt động và nêu 1 ví dụ thực tế minh họa trong môn {subjectName}.",

            _ =>
                $"🤖 PHÂN TÍCH & GIẢI THÍCH TỪ TRỢ LÝ AI ({docContext})\n" +
                $"• Câu hỏi của bạn: {prompt}\n" +
                $"• Giải thích chi tiết:\n" +
                $"  1. Bản chất khái niệm: Đây là kiến thức nền tảng trong {subjectName}, giúp kết nối lý thuyết với bài tập thực hành.\n" +
                $"  2. Các điểm cần lưu ý: Chú ý điều kiện áp dụng, ưu/nhược điểm và các trường hợp biên thường gặp trong đề thi.\n" +
                $"  3. Gợi ý ghi nhớ: Hãy ghim tóm tắt này lên Bảng Blueprint cạnh trang tài liệu để đối chiếu nhanh khi đọc slide."
        };
    }

    [RelayCommand]
    public async Task SaveEditedAiHistoryAsync()
    {
        if (SelectedAiHistory == null) return;

        try
        {
            IsBusy = true;
            SelectedAiHistory.NoteEntity.OcrText = SelectedAiQuestionDetail.Trim();
            SelectedAiHistory.NoteEntity.AiResponse = SelectedAiAnswerDetail.Trim();
            await _repository.UpdateNoteAsync(SelectedAiHistory.NoteEntity);
            ApplyAiHistoryFilter();
            StatusMessage = "Đã cập nhật nội dung trong Lịch sử hỏi AI.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật lịch sử AI: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSelectedAiHistoryAsync()
    {
        if (SelectedAiHistory == null) return;

        if (Application.Current?.Dispatcher.CheckAccess() == true &&
            Application.Current?.MainWindow is { IsLoaded: true })
        {
            var confirm = MessageBox.Show(
                $"Xóa mục hỏi AI \"{SelectedAiHistory.Title}\" khỏi lịch sử môn học?",
                "Xác nhận xóa lịch sử AI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;
        }

        try
        {
            IsBusy = true;
            await _repository.DeleteNoteAsync(SelectedAiHistory.Id);
            _allAiHistoryCache.Remove(SelectedAiHistory);
            SelectedAiHistory = null;
            ApplyAiHistoryFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xóa lịch sử AI: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Ghim câu hỏi &amp; câu trả lời đang xem trong Lịch sử AI thành một Note Pinned trên Bảng Blueprint.
    /// </summary>
    [RelayCommand]
    public async Task PinAiHistoryToCanvasAsync()
    {
        if (ActiveSubject == null || SelectedAiHistory == null) return;

        try
        {
            IsBusy = true;
            var card = new Flashcard
            {
                SubjectId = ActiveSubject.Id,
                SnippetNoteId = SelectedAiHistory.Id,
                Question = $"[AI Note] {SelectedAiHistory.Title}",
                Answer = SelectedAiAnswerDetail.Trim(),
                MasteryStatus = MasteryStatus.Learning,
                LastReviewedAt = DateTime.Now
            };

            var saved = await _repository.AddFlashcardAsync(card);
            int idx = PinnedFlashcards.Count;
            double initX = 770 + ((idx % 2) * 285);
            double initY = 48 + ((idx / 2) * 225);
            PinnedFlashcardItem.SavePinLayout(saved.Id, initX, initY, "#DBEAFE");

            var pinned = PinnedFlashcardItem.FromEntity(saved, idx);
            _allPinnedCardsCache.Add(pinned);
            PinnedFlashcards.Add(pinned);
            PinnedFlashcardsCount = PinnedFlashcards.Count;
            HasPinnedFlashcards = PinnedFlashcardsCount > 0;
            UpdateBoardCanvasHeight();

            SelectedTab = "Documents";
            StatusMessage = "📌 Đã ghim nội dung AI thành Note trên Bảng Blueprint!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể ghim lên Bảng Blueprint: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}
