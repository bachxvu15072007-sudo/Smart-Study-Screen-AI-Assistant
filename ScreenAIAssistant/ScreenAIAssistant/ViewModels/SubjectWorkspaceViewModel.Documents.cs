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
/// Phần logic: Bảng Blueprint tài liệu và Flashcard ghim.
/// </summary>
public partial class SubjectWorkspaceViewModel
{
    #region Logic 2: Bảng Blueprint Tài liệu & Cửa sổ Popup Ghim Flashcard (Note Pinned Kéo Thả Tự Do)

    partial void OnDocumentSearchKeywordChanged(string value) => ApplyDocumentFilter();

    partial void OnSelectedDocumentChanged(SubjectDocumentItem? value)
    {
        // Xóa ngay Flashcard ghim của tài liệu trước để không hiển thị nhầm trong lúc nạp tài liệu mới
        _allPinnedCardsCache.Clear();
        PinnedFlashcards.Clear();
        PinnedFlashcardsCount = 0;
        HasPinnedFlashcards = false;

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

        // Tự động nạp các Flashcard / Note ghim tương ứng với tài liệu đang mở
        _ = LoadPinnedFlashcardsAsync();
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

            // Người dùng đã chuyển sang tài liệu khác trong lúc đang nạp: bỏ kết quả cũ
            if (!ReferenceEquals(SelectedDocument, docItem)) return;

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

    [RelayCommand]
    public void FitWidthCanvas()
    {
        CanvasZoom = 1.25;
        CanvasZoomPercentText = "125%";
    }

    /// <summary>
    /// Nạp danh sách Flashcard được ghim (Note Pinned) tương ứng với tài liệu đang mở (DocumentId).
    /// </summary>
    private async Task LoadPinnedFlashcardsAsync()
    {
        if (ActiveSubject == null) return;

        if (SelectedDocument == null)
        {
            _allPinnedCardsCache.Clear();
            PinnedFlashcards.Clear();
            PinnedFlashcardsCount = 0;
            HasPinnedFlashcards = false;
            UpdateBoardCanvasHeight();
            return;
        }

        var requestedDocument = SelectedDocument;
        try
        {
            var cards = await _repository.GetFlashcardsAsync(ActiveSubject.Id, documentId: requestedDocument.Id);

            // Người dùng đã chuyển sang tài liệu khác trong lúc truy vấn: bỏ kết quả cũ
            if (!ReferenceEquals(SelectedDocument, requestedDocument)) return;

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
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể nạp Flashcard ghim của tài liệu: {ex.Message}";
        }
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
                DocumentId = SelectedDocument?.Id,
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
            StatusMessage = "📌 Đã lưu Flashcard gắn liền với tài liệu này trong Study Margin!";
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
}
