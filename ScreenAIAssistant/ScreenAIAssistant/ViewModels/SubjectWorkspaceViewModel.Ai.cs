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
/// Phần logic: Trợ lý AI và lịch sử hỏi AI.
/// </summary>
public partial class SubjectWorkspaceViewModel
{
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
