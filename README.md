# 🎓 Smart Study & Screen AI Assistant (`ScreenAIAssistant`)

Ứng dụng Windows Desktop xây dựng trên nền tảng **WPF (.NET 10)** hỗ trợ sinh viên quản lý học tập: chụp nhanh vùng tài liệu/bài giảng trên màn hình, trích xuất chữ bằng **Windows OCR**, dùng **Google Gemini AI** để dịch thuật/tóm tắt và tự động tạo bộ thẻ **Flashcard** ôn tập.

Toàn bộ dữ liệu (**3 bảng liên kết `1-N`: Môn học, Ghi chú OCR, Flashcard**) được lưu trữ cục bộ 100% bằng **SQLite** với đầy đủ các chức năng Thêm, Xem, Sửa, Xóa, Tìm kiếm và Thống kê.

---

## 📌 Tính năng nổi bật (3 Phân hệ cho 3 Thành viên)

- **📊 Phân hệ 1 – Quản lý Môn học & Thống kê (`DashboardView` - Bảng `Subject`):**
  - Thêm, sửa, xóa danh sách các môn học/chủ đề kèm mã màu trực quan.
  - Thống kê trực quan với **3 thẻ số liệu lớn** (Tổng số Môn học, Tổng số Ghi chú, Tổng số Flashcard) đặt trong `WrapPanel` co giãn tự động và thanh tiến độ **`ProgressBar` chuẩn của WPF** (tùy biến `ControlTemplate`) hiển thị tỷ lệ thuộc bài của từng môn.
- **📝 Phân hệ 2 – Chụp màn hình, OCR & Quản lý Ghi chú (`StudyNotesView` - Bảng `SnippetNote`):**
  - **Chụp vùng màn hình (`OverlayWindow`):** Cửa sổ phủ mờ toàn màn hình (`Canvas` + `CombinedGeometry`), tự động cân chỉnh DPI (100%–150%) hoặc dán ảnh từ Clipboard (`Ctrl + V`), trích xuất chữ tức thì bằng `Windows.Media.Ocr`.
  - **Trợ lý AI (`ComboBox` 4 chế độ có sẵn):** Chọn nhanh *1. Dịch sang Tiếng Việt*, *2. Tóm tắt ý chính*, *3. Giải thích thuật ngữ/code*, *4. Tạo câu hỏi ôn tập* để Gemini AI xử lý bất đồng bộ.
  - **CRUD Ghi chú:** Giao diện Master-Detail (`Grid` + `GridSplitter`), cho phép sửa lại chữ OCR, thêm ghi chú cá nhân, đánh dấu sao yêu thích và tìm kiếm/lọc theo môn học.
- **🗂️ Phân hệ 3 – Quản lý & Lật thẻ Ôn tập (`FlashcardView` - Bảng `Flashcard`):**
  - Thêm, sửa, xóa thẻ ghi nhớ (*Mặt trước: Câu hỏi / Mặt sau: Đáp án*) thủ công hoặc bấm **"✨ AI Tự tạo Flashcard"** trực tiếp từ nội dung ghi chú.
  - Chế độ **Lật thẻ ôn tập (Flip Card)** và cập nhật mức độ thuộc bài (*Chưa thuộc / Đang học / Đã thuộc*).

---

## 🛠️ Công nghệ & Kỹ thuật WPF áp dụng

- **Nền tảng:** .NET 10 (`net10.0-windows10.0.19041.0`) / WPF (C#)
- **Kiến trúc:** MVVM (`CommunityToolkit.Mvvm`) + Dependency Injection (`Microsoft.Extensions.DependencyInjection`)
- **CSDL:** SQLite + Entity Framework Core (3 bảng quan hệ `1-N`: `Subject`, `SnippetNote`, `Flashcard`)
- **Giao diện & Template:** `DockPanel`, `Grid` + `GridSplitter`, `WrapPanel`, `StackPanel`, `Canvas`, `ResourceDictionary`, `DataTemplate`, `ControlTemplate`, `IValueConverter`
- **Đồ họa & Đa luồng:** `CombinedGeometry`, cắt `Bitmap` chuẩn DPI, bất đồng bộ `async/await` với `Windows.Media.Ocr` & `HttpClient` (Google Gemini REST API)

---

## 👥 Phân công Nhóm (3 Thành viên)

| Thành viên | Phụ trách Bảng & Màn hình | Nội dung công việc chính |
| :--- | :--- | :--- |
| **Thành viên 1** | Bảng `SnippetNote` + `StudyNotesView` & `OverlayWindow` | Chụp màn hình đồ họa 2D (`Canvas`, DPI), `WindowsOcrService`, CRUD Ghi chú & Tìm kiếm/Lọc |
| **Thành viên 2** | Bảng `Flashcard` + `FlashcardView` & `GeminiApiService` | Gọi Gemini REST API (`async/await`), AI tự sinh Flashcard, CRUD Thẻ nhớ & Chế độ Lật thẻ ôn tập |
| **Thành viên 3** | Bảng `Subject` + `DashboardView` & `MainWindow` | Khởi tạo SQLite (`AppDbContext`), CRUD Môn học, Thống kê (3 thẻ số liệu + `ProgressBar`) & Bộ `Resources/*.xaml` |

---

## 📁 Cấu trúc thư mục (`ScreenAIAssistant`)

- `Models/`: Thực thể dữ liệu (`Subject.cs`, `SnippetNote.cs`, `Flashcard.cs`, `MasteryStatus.cs`)
- `Data/`: Tầng CSDL SQLite (`AppDbContext.cs`, `IStudyRepository.cs`, `StudyRepository.cs`)
- `Services/`: Dịch vụ nghiệp vụ (`ScreenCaptureService.cs`, `WindowsOcrService.cs`, `GeminiApiService.cs`)
- `ViewModels/`: Logic MVVM (`MainViewModel.cs`, `DashboardViewModel.cs`, `StudyNotesViewModel.cs`, `FlashcardViewModel.cs`)
- `Views/`: Giao diện XAML (`MainWindow.xaml`, `OverlayWindow.xaml`, `DashboardView.xaml`, `StudyNotesView.xaml`, `FlashcardView.xaml`)
- `Resources/`: Tài nguyên giao diện (`Colors.xaml`, `Styles.xaml`, `Templates.xaml`)
- `Helpers/`: Các bộ chuyển đổi dữ liệu (`*Converter.cs`, `DpiHelper.cs`)
