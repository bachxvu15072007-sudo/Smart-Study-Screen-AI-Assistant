using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.Data;

/// <summary>
/// Lớp đóng gói dữ liệu thống kê tiến độ học tập của từng môn học (Dùng cho DashboardView).
/// </summary>
public class SubjectProgress
{
    public Subject Subject { get; set; } = null!;
    public int NotesCount { get; set; }
    public int FlashcardsCount { get; set; }
    public int MasteredCount { get; set; }
    public double MasteryPercentage => FlashcardsCount > 0
        ? Math.Round((double)MasteredCount * 100.0 / FlashcardsCount, 1)
        : 0.0;
}

/// <summary>
/// Interface cung cấp các phương thức truy xuất dữ liệu SQLite bất đồng bộ (IStudyRepository).
/// Hợp đồng giao tiếp chung cho cả 3 thành viên trong nhóm.
/// </summary>
public interface IStudyRepository
{
    #region 1. Phân hệ Môn học (Subject - Thành viên 3)
    Task<List<Subject>> GetAllSubjectsAsync();
    Task<Subject?> GetSubjectByIdAsync(int id);
    Task<Subject> AddSubjectAsync(Subject subject);
    Task UpdateSubjectAsync(Subject subject);
    Task DeleteSubjectAsync(int id);
    Task<bool> IsSubjectNameExistsAsync(string name, int? excludeId = null);
    #endregion

    #region 2. Phân hệ Ghi chú OCR & AI (SnippetNote - Thành viên 1)
    Task<List<SnippetNote>> GetNotesAsync(int? subjectId = null, string? searchKeyword = null, bool? onlyFavorites = null);
    Task<SnippetNote?> GetNoteByIdAsync(int id);
    Task<SnippetNote> AddNoteAsync(SnippetNote note);
    Task UpdateNoteAsync(SnippetNote note);
    Task DeleteNoteAsync(int id);
    #endregion

    #region 3. Phân hệ Thẻ ghi nhớ Flashcard (Flashcard - Thành viên 2)
    Task<List<Flashcard>> GetFlashcardsAsync(int? subjectId = null, MasteryStatus? status = null, string? searchKeyword = null, int? documentId = null);
    Task<Flashcard?> GetFlashcardByIdAsync(int id);
    Task<Flashcard> AddFlashcardAsync(Flashcard flashcard);
    Task AddFlashcardsRangeAsync(IEnumerable<Flashcard> flashcards);
    Task UpdateFlashcardAsync(Flashcard flashcard);
    Task UpdateMasteryStatusAsync(int flashcardId, MasteryStatus newStatus);
    Task DeleteFlashcardAsync(int id);
    #endregion

    #region 4. Thống kê Dashboard (Thành viên 3)
    Task<int> GetTotalSubjectsCountAsync();
    Task<int> GetTotalNotesCountAsync();
    Task<int> GetTotalFlashcardsCountAsync();
    Task<int> GetMasteredFlashcardsCountAsync();
    Task<List<SubjectProgress>> GetSubjectProgressListAsync();
    #endregion

    #region 5. Quản lý Tài liệu Học tập của Môn học (SubjectDocument - Thành viên 3)
    Task<List<SubjectDocument>> GetSubjectDocumentsAsync(int subjectId);
    Task<SubjectDocument> AddSubjectDocumentAsync(SubjectDocument document);
    Task UpdateSubjectDocumentAsync(SubjectDocument document);
    Task DeleteSubjectDocumentAsync(int id);
    #endregion

    #region 6. Quản lý Phiên học & Thống kê Giờ học (StudySession - Thành viên 3)
    Task<List<StudySession>> GetStudySessionsAsync(int? subjectId = null);
    Task<StudySession> AddStudySessionAsync(StudySession session);
    Task DeleteStudySessionAsync(int id);
    Task<double> GetTotalStudyHoursThisWeekAsync();
    #endregion
}
