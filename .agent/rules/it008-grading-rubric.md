# Quy Tắc Đảm Bảo Điểm Tối Đa Theo Tiêu Chí Chấm Đồ Án IT008 (`it008-grading-rubric`)

Mỗi khi thiết kế hoặc chỉnh sửa một tính năng trong `ScreenAIAssistant`, Agent phải kiểm tra đối chiếu với bảng tiêu chí chấm điểm của môn **Lập Trình Trực Quan (IT008)**:

## 1. Đảm bảo CRUD & Bắt lỗi Nhập liệu sai (Tiêu chí 1 - 2.0 điểm)
- **CRUD đầy đủ cho cả 3 bảng (`Subject`, `SnippetNote`, `Flashcard`):**
  - Mọi màn hình quản lý phải có đủ 4 thao tác: **Thêm mới (Create)**, **Xem & Tìm kiếm/Lọc (Read/Filter)**, **Cập nhật (Update)**, **Xóa có hộp thoại xác nhận (Delete)**.
- **Xử lý các trường hợp nhập liệu sai (Validation):**
  - Không bao giờ lưu dữ liệu rỗng vào SQLite: Kiểm tra `string.IsNullOrWhiteSpace` cho Tên môn học (`Subject.Name`), Tiêu đề ghi chú (`SnippetNote.Title`), Câu hỏi/Đáp án (`Flashcard.Question`, `Flashcard.Answer`).
  - Kiểm tra vùng chụp màn hình: Nếu `Width < 15` hoặc `Height < 15` thì hủy thao tác chụp.
  - Kiểm tra lỗi ngoại lệ khi gọi OCR và Gemini AI: Nếu ảnh không có chữ, thiếu API Key hoặc mất kết nối Internet, phải bắt `try/catch` và gán thông báo lỗi tiếng Việt rõ ràng vào thuộc tính `ErrorMessage` trên ViewModel (không để ứng dụng bị crash).

## 2. Đảm bảo Bố cục Giao diện, Co giãn & Styles (Tiêu chí 2 - 2.0 điểm)
- **Kết hợp đa dạng Layout chuẩn WPF:**
  - `MainWindow.xaml`: Dùng `DockPanel` cho thanh điều hướng bên trái và vùng nội dung chính.
  - `DashboardView.xaml`: Dùng `WrapPanel` cho 3 thẻ số liệu thống kê (để tự rớt dòng khi thu hẹp cửa sổ) và `Grid` cho danh sách môn học kèm `ProgressBar`.
  - `StudyNotesView.xaml` & `FlashcardView.xaml`: Dùng `Grid` chia cột Master-Detail kết hợp `GridSplitter` cho phép người dùng kéo giãn độ rộng cột.
  - `OverlayWindow.xaml`: Dùng `Canvas` và `Path` (`CombinedGeometry`) để vẽ vùng quét màn hình.
- **Khả năng Co giãn cửa sổ (Responsive Resize):**
  - Không hardcode `Width` và `Height` cố định cho các vùng chứa chính; luôn sử dụng `*`, `Auto`, `MinWidth`, `MinHeight` và `ScrollViewer`.
- **Sử dụng `Style`, `ResourceDictionary`, `Template` & `IValueConverter`:**
  - Mọi màu sắc, kiểu nút bấm, kiểu ô nhập liệu và `ControlTemplate` (như thanh `ProgressBar` bo tròn) phải được đặt trong `Resources/Colors.xaml`, `Styles.xaml`, `Templates.xaml` và tham chiếu qua `StaticResource` / `DynamicResource`.
