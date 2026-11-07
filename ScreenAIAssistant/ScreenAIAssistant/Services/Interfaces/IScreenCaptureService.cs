using System.Drawing;

namespace ScreenAIAssistant.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ chụp ảnh vùng màn hình và nạp ảnh tài liệu (Do Thành viên 1 phụ trách).
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Chụp vùng màn hình theo tọa độ vật lý (đã tính tỷ lệ DPI) và lưu vào ổ cứng cục bộ.
    /// </summary>
    /// <param name="physicalBounds">Tọa độ và kích thước vật lý của vùng cần chụp</param>
    /// <returns>Đường dẫn file ảnh (.png) đã lưu</returns>
    Task<string> CaptureRegionAsync(Rectangle physicalBounds);

    /// <summary>
    /// Nạp ảnh từ file trên máy người dùng thông qua OpenFileDialog.
    /// </summary>
    /// <returns>Đường dẫn file ảnh hợp lệ, hoặc null nếu người dùng hủy</returns>
    Task<string?> ImportFromFileAsync();

    /// <summary>
    /// Nạp ảnh từ Clipboard (Ctrl + V) và lưu vào bộ nhớ cục bộ.
    /// </summary>
    /// <returns>Đường dẫn file ảnh đã lưu, hoặc null nếu Clipboard không có ảnh</returns>
    Task<string?> ImportFromClipboardAsync();
}
