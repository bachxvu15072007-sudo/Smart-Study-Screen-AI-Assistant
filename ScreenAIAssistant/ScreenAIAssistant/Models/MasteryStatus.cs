namespace ScreenAIAssistant.Models;

/// <summary>
/// Mức độ thuộc bài của thẻ ghi nhớ Flashcard.
/// </summary>
public enum MasteryStatus
{
    /// <summary>
    /// Thẻ mới tạo, chưa thuộc (0)
    /// </summary>
    New = 0,

    /// <summary>
    /// Đang trong quá trình học và ôn tập (1)
    /// </summary>
    Learning = 1,

    /// <summary>
    /// Đã thuộc bài nhuần nhuyễn (2)
    /// </summary>
    Mastered = 2
}
