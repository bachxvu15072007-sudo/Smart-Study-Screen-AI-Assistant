namespace ScreenAIAssistant.Models;

/// <summary>
/// Thực thể Môn học / Chủ đề học tập (Bảng Subject).
/// </summary>
public class Subject
{
    public int Id { get; set; }

    /// <summary>
    /// Tên môn học hoặc chuyên đề (VD: Lập trình trực quan IT008).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mã màu hiển thị đại diện trên giao diện (Hex color, VD: #2563EB).
    /// </summary>
    public string ColorHex { get; set; } = "#2563EB";

    /// <summary>
    /// Mô tả chi tiết hoặc ghi chú về môn học.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm khởi tạo môn học.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public ICollection<SubjectDocument> Documents { get; set; } = new List<SubjectDocument>();
    public ICollection<SnippetNote> Notes { get; set; } = new List<SnippetNote>();
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
}
