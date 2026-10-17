# 📸 Kế Hoạch Chi Tiết — Thành Viên 1: Chụp Màn Hình, OCR & Quản Lý Ghi Chú (`SnippetNote`)

## 1. Phạm vi Phụ trách
- **Bảng CSDL:** `SnippetNote` (Ghi chú chụp màn hình OCR & kết quả AI).
- **Màn hình (Views):**
  - `Views/OverlayWindow.xaml` & `OverlayWindow.xaml.cs`
  - `Views/StudyNotesView.xaml` & `StudyNotesView.xaml.cs`
- **ViewModel:** `ViewModels/StudyNotesViewModel.cs`
- **Services & Helpers:**
  - `Services/Interfaces/IScreenCaptureService.cs` & `Services/ScreenCaptureService.cs`
  - `Services/Interfaces/IOcrService.cs` & `Services/WindowsOcrService.cs`
  - `Helpers/DpiHelper.cs` & `Helpers/ImagePathToBitmapConverter.cs`

---

## 2. Các Hạng mục Công việc Cụ thể (Checklist)

### Bước 1: Xây dựng `OverlayWindow` & `ScreenCaptureService` (Đồ họa 2D & DPI)
- [ ] Thiết kế `OverlayWindow.xaml` không viền (`WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"`, `WindowState="Maximized"`).
- [ ] Sử dụng `Path` kết hợp `CombinedGeometry` (`GeometryCombineMode.Exclude`) giữa `RectangleGeometry` toàn màn hình và `RectangleGeometry` vùng chọn để làm tối vùng ngoài nhưng giữ trong suốt vùng đang quét chuột.
- [ ] Viết `DpiHelper.cs` sử dụng `VisualTreeHelper.GetDpi(window)` để quy đổi tọa độ chuột trên `Canvas` (DIPs) sang Physical Pixels.
- [ ] Kiểm tra kích thước vùng chọn: nếu `Width < 15` hoặc `Height < 15` thì hủy thao tác chụp.
- [ ] Lưu ảnh chụp vào thư mục `%LocalAppData%\ScreenAIAssistant\Images\` và trả về đường dẫn `ImagePath`.
- [ ] Hỗ trợ thêm 2 phương thức nạp ảnh an toàn: Chọn file ảnh (`.png`, `.jpg`) qua `OpenFileDialog` và dán ảnh từ `Clipboard.GetImage()` (`Ctrl + V`).

### Bước 2: Xây dựng `WindowsOcrService` (Đa luồng WinRT)
- [ ] Cài đặt hàm `ExtractTextAsync(string imagePath)` sử dụng `Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages()`.
- [ ] Chạy bất đồng bộ (`async/await`), trả về chuỗi văn bản trích xuất được. Nếu ảnh không có chữ, trả về chuỗi rỗng để `StudyNotesViewModel` hiển thị cảnh báo thân thiện.

### Bước 3: Xây dựng `StudyNotesView` & `StudyNotesViewModel` (MVVM & CRUD)
- [ ] Thiết kế bố cục **Master-Detail** bằng `Grid` và `GridSplitter` (cho phép kéo giãn cột danh sách ghi chú và vùng chi tiết).
- [ ] Thanh công cụ phía trên: Chọn Môn học (`Subject`), ô tìm kiếm từ khóa (`SearchKeyword`), nút lọc Yêu thích (`IsFavorite`), nút **"✂️ Chụp màn hình"**, nút **"📋 Dán ảnh"**.
- [ ] Vùng chi tiết bên phải:
  - Hiển thị ảnh chụp (`Image` binding qua `ImagePathToBitmapConverter`).
  - Ô `TextBox` hiển thị và cho phép sửa lại `OcrText`.
  - `ComboBox` cố định 4 chế độ AI:
    1. *"Dịch sang Tiếng Việt"*
    2. *"Tóm tắt ý chính"*
    3. *"Giải thích thuật ngữ / Code"*
    4. *"Tạo câu hỏi ôn tập"*
  - Nút **"🤖 Chạy AI"** (gọi `IAiAssistantService` của Thành viên 2) và nút **"✨ Tạo Flashcard"** (lưu thẳng các cặp câu hỏi/đáp án sang bảng `Flashcard`).
  - Nút **Lưu mới / Cập nhật (Create/Update)** và **Xóa (Delete)** bản ghi `SnippetNote`.

---

## 3. Tiêu chí Nghiệm thu & Câu hỏi Vấn đáp cần nắm vững
1. **Giải thích cách xử lý lệch vùng chụp khi màn hình laptop để Scale 125% hoặc 150%?**  
   $\rightarrow$ Giải thích sự khác biệt giữa đơn vị thiết kế của WPF (1 DIP = 1/96 inch) và điểm ảnh vật lý của màn hình khi dùng `CopyFromScreen`, cách lấy tỷ lệ qua `VisualTreeHelper.GetDpi`.
2. **Làm sao giữ đúng kiến trúc MVVM khi `OverlayWindow` phải bắt sự kiện chuột `MouseDown/Move/Up`?**  
   $\rightarrow$ Sự kiện chuột ở `OverlayWindow.xaml.cs` chỉ dùng để vẽ khung chữ nhật thuần giao diện (View-only), sau khi thả chuột chỉ trả về tọa độ `Rect` cho `StudyNotesViewModel` xử lý nghiệp vụ cắt ảnh, gọi OCR và lưu DB.
