using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenAIAssistant.Models;

/// <summary>
/// Thực thể lưu trữ thông tin Tài liệu Học tập (.pdf, .docx, .pptx) của từng Môn học trong CSDL SQLite.
/// </summary>
public class SubjectDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [ForeignKey(nameof(SubjectId))]
    public Subject? Subject { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Loại định dạng tài liệu: "PDF", "WORD", "PPT"
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string FileType { get; set; } = "PDF";

    public long FileSizeKb { get; set; }

    public int PageCount { get; set; } = 1;

    /// <summary>
    /// Nội dung văn bản trích xuất từ tài liệu (nếu có).
    /// </summary>
    public string ExtractedContent { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú cá nhân của người học khi đọc tài liệu này.
    /// </summary>
    public string PersonalNote { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
