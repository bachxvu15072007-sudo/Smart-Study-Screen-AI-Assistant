namespace ScreenAIAssistant.Models;

/// <summary>
/// Thực thể Thẻ ghi nhớ ôn tập Flashcard (Bảng Flashcard).
/// </summary>
public class Flashcard
{
    public int Id { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới môn học (Subject).
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới ghi chú gốc sinh ra thẻ (Nullable nếu tạo thủ công).
    /// </summary>
    public int? SnippetNoteId { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết tới Tài liệu học tập (SubjectDocument) sinh ra hoặc đính kèm thẻ (Nullable).
    /// </summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// Mặt trước: Câu hỏi / Thuật ngữ / Khái niệm.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Mặt sau: Câu trả lời / Giải nghĩa súc tích.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Mức độ thuộc bài: 0: Chưa thuộc, 1: Đang học, 2: Đã thuộc.
    /// </summary>
    public MasteryStatus MasteryStatus { get; set; } = MasteryStatus.New;

    /// <summary>
    /// Thời điểm gần nhất người dùng ôn tập thẻ này.
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    // Navigation properties
    public Subject? Subject { get; set; }
    public SnippetNote? SnippetNote { get; set; }
    public SubjectDocument? Document { get; set; }
}
