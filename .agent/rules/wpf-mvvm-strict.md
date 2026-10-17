# Quy Tắc Kiến Trúc WPF & MVVM Nghiêm Ngặt (`wpf-mvvm-strict`)

Mọi đoạn mã C# và XAML được tạo ra trong dự án `ScreenAIAssistant` bắt buộc tuân thủ các quy tắc sau:

## 1. Tuân thủ MVVM Tuyệt đối (Tiêu chí 3 - 3.0 điểm)
- **Không viết logic nghiệp vụ trong Code-behind (`.xaml.cs`):**
  - Các file `MainWindow.xaml.cs`, `DashboardView.xaml.cs`, `StudyNotesView.xaml.cs`, `FlashcardView.xaml.cs` chỉ được phép chứa `InitializeComponent()`.
  - Ngoại lệ duy nhất: `OverlayWindow.xaml.cs` được phép xử lý sự kiện chuột (`MouseDown`, `MouseMove`, `MouseUp`) để vẽ hình chữ nhật chọn vùng thuần giao diện trên `Canvas`, sau đó trả kết quả `Rect` về cho ViewModel xử lý cắt ảnh và gọi OCR.
- **Sử dụng `CommunityToolkit.Mvvm`:**
  - Tất cả ViewModel kế thừa từ `ViewModelBase` (kế thừa `ObservableObject`).
  - Sử dụng Source Generators `[ObservableProperty]` cho các thuộc tính binding và `[RelayCommand]` cho các thao tác nút bấm (`ICommand` / `IAsyncRelayCommand`).
- **Tập hợp dữ liệu động:**
  - Danh sách hiển thị lên `ListBox`, `ItemsControl`, `DataGrid` hoặc `ComboBox` bắt buộc dùng `ObservableCollection<T>` để tự động đồng bộ UI khi Thêm/Sửa/Xóa.

## 2. Đa luồng & Không làm đơ Giao diện (Tiêu chí 3' - 3.0 điểm)
- Mọi tác vụ I/O gồm:
  1. Nhận diện chữ (`WindowsOcrService`)
  2. Gọi API mạng (`GeminiApiService`)
  3. Truy vấn cơ sở dữ liệu SQLite (`StudyRepository`)
  **BẮT BUỘC** viết dưới dạng bất đồng bộ (`async Task` / `async/await`).
- Luôn bật cờ `IsBusy = true` trước khi chạy tác vụ nặng và `IsBusy = false` trong khối `finally` để hiển thị trạng thái đang xử lý trên UI.

## 3. Quy ước Đặt tên & Tổ chức Code (Tiêu chí 4 - 1.0 điểm)
- **Class, Method, Property, Public/Protected:** `PascalCase` (VD: `StudyNotesViewModel`, `LoadNotesAsync`).
- **Biến thành viên Private (Field):** `_camelCase` (VD: `[ObservableProperty] private string _searchKeyword = string.Empty;`).
- **Interface:** Bắt đầu bằng chữ `I` (VD: `IStudyRepository`, `IOcrService`, `IAiAssistantService`).
- **Quản lý Tài nguyên Đồ họa:** Mọi đối tượng `Bitmap`, `Graphics`, `MemoryStream`, `FileStream` phải được đặt trong khối `using` để giải phóng bộ nhớ ngay lập tức, tránh rò rỉ RAM (Memory Leak).
