namespace ScreenAIAssistant.Models;

/// <summary>
/// Thực thể Ghi chú chụp màn hình & AI Assistant (Bảng SnippetNote).
/// </summary>
public class SnippetNote
{
    public int Id { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới môn học (Subject).
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Tiêu đề ghi chú.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn file ảnh chụp lưu trên ổ cứng cục bộ (%LocalAppData%).
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Văn bản nhận diện được từ Windows.Media.Ocr (Cho phép sửa tay).
    /// </summary>
    public string OcrText { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả xử lý từ Gemini AI (Dịch thuật, Tóm tắt, Giải thích code).
    /// </summary>
    public string AiResponse { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú cá nhân bổ sung của sinh viên.
    /// </summary>
    public string PersonalNote { get; set; } = string.Empty;

    /// <summary>
    /// Đánh dấu ghi chú quan trọng / yêu thích.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Thời điểm tạo ghi chú.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public Subject? Subject { get; set; }
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
}
