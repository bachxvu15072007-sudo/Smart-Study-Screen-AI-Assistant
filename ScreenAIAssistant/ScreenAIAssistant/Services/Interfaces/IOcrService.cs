namespace ScreenAIAssistant.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ trích xuất văn bản từ hình ảnh sử dụng Windows.Media.Ocr (Do Thành viên 1 phụ trách).
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Nhận diện chữ từ file ảnh bất đồng bộ.
    /// </summary>
    /// <param name="imagePath">Đường dẫn file ảnh trên ổ cứng</param>
    /// <returns>Chuỗi văn bản trích xuất được (hoặc chuỗi rỗng nếu ảnh không có chữ)</returns>
    Task<string> ExtractTextAsync(string imagePath);
}
