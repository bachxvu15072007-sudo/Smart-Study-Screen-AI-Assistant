using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ trợ lý trí tuệ nhân tạo Gemini REST API (Do Thành viên 2 phụ trách).
/// </summary>
public interface IAiAssistantService
{
    /// <summary>
    /// Xử lý văn bản theo 1 trong 4 chế độ ComboBox (Dịch thuật, Tóm tắt, Giải thích code, Tạo câu hỏi).
    /// </summary>
    /// <param name="ocrText">Văn bản cần xử lý</param>
    /// <param name="mode">Tên chế độ tương ứng</param>
    /// <param name="cancellationToken">Token hỗ trợ hủy tác vụ khi cần</param>
    /// <returns>Nội dung phản hồi từ AI</returns>
    Task<string> ProcessTextAsync(string ocrText, string mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tự động sinh danh sách Flashcard dạng mảng JSON từ nội dung ghi chú và bóc tách thành đối tượng.
    /// </summary>
    /// <param name="ocrText">Nội dung bài học hoặc ghi chú</param>
    /// <param name="subjectId">Mã môn học để gán cho thẻ nhớ</param>
    /// <param name="cancellationToken">Token hỗ trợ hủy tác vụ khi cần</param>
    /// <returns>Danh sách các thẻ Flashcard được tạo bởi AI</returns>
    Task<List<Flashcard>> GenerateFlashcardsAsync(string ocrText, int subjectId, CancellationToken cancellationToken = default);
}
