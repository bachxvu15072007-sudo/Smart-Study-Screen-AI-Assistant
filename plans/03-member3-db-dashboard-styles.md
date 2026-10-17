# 📊 Kế Hoạch Chi Tiết — Thành Viên 3: CSDL SQLite, Quản Lý Môn Học (`Subject`), Dashboard Thống Kê & Bộ Style/Template

## 1. Phạm vi Phụ trách
- **Bảng CSDL:** Thiết lập toàn bộ SQLite (`AppDbContext`, `StudyRepository`) và phụ trách trực tiếp CRUD bảng `Subject` (Môn học).
- **Màn hình (Views):**
  - `Views/MainWindow.xaml` & `MainWindow.xaml.cs`
  - `Views/DashboardView.xaml` & `DashboardView.xaml.cs`
- **ViewModels:**
  - `ViewModels/ViewModelBase.cs`
  - `ViewModels/MainViewModel.cs`
  - `ViewModels/DashboardViewModel.cs`
- **Tài nguyên Giao diện (Resources & Helpers):**
  - `Resources/Colors.xaml`, `Resources/Styles.xaml`, `Resources/Templates.xaml`
  - `Helpers/BoolToVisibilityConverter.cs`

---

## 2. Các Hạng mục Công việc Cụ thể (Checklist)

### Bước 1: Khởi tạo CSDL SQLite 3 Bảng & Dependency Injection (`App.xaml.cs`)
- [ ] Cài đặt các gói NuGet trong `ScreenAIAssistant.csproj` và chỉnh `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`.
- [ ] Tạo 3 lớp thực thể: `Subject.cs`, `SnippetNote.cs`, `Flashcard.cs` và enum `MasteryStatus.cs`.
- [ ] Xây dựng `AppDbContext.cs` cấu hình khóa ngoại `1-N` (`Subject` $\rightarrow$ `SnippetNote`, `Subject` $\rightarrow$ `Flashcard`) và tự động tạo dữ liệu mẫu (`EnsureCreated` + Seed Data gồm 2 môn học mẫu, vài ghi chú và thẻ nhớ mẫu) để mở app lên là có sẵn dữ liệu đẹp phục vụ Demo.
- [ ] Xây dựng `StudyRepository.cs` thực thi các hàm CRUD và truy vấn thống kê bằng LINQ.
- [ ] Cấu hình `ServiceCollection` trong `App.xaml.cs` để tiêm (Inject) Repository, Services và ViewModels.

### Bước 2: Xây dựng `MainWindow` & Bộ `ResourceDictionary`
- [ ] Thiết kế `MainWindow.xaml` sử dụng `DockPanel`:
  - Cột bên trái (Sidebar): Các nút điều hướng chuyển đổi giữa 3 màn hình (`📊 Tổng quan & Môn học`, `📝 Ghi chú OCR & AI`, `🗂️ Ôn tập Flashcard`) + Ô nhập nhanh `Gemini API Key` ở góc dưới Sidebar.
  - Vùng trung tâm: `ContentControl` hiển thị `CurrentViewModel`.
- [ ] Xây dựng `Resources/Colors.xaml` và `Resources/Styles.xaml` định nghĩa màu sắc, font chữ, kiểu nút bấm (`PrimaryButtonStyle`, `DangerButtonStyle`), ô nhập liệu (`ModernTextBoxStyle`) và khung thẻ (`CardBorderStyle`).

### Bước 3: Xây dựng `DashboardView` & `DashboardViewModel` (CRUD `Subject` & Thống kê)
- [ ] **Khu vực 1 – 3 Thẻ số liệu tổng quan (`WrapPanel`):**
  1. 📚 **Tổng số Môn học**
  2. 📝 **Tổng số Ghi chú OCR**
  3. 🎯 **Tổng số Thẻ Flashcard (kèm % Đã thuộc)**
  *(Sử dụng `WrapPanel` để khi kéo thu nhỏ cửa sổ, 3 thẻ tự động co giãn và rớt dòng mượt mà).*
- [ ] **Khu vực 2 – Quản lý Môn học (`Subject` CRUD) & Tiến độ từng môn (`ProgressBar`):**
  - Danh sách các môn học, mỗi môn hiển thị số lượng ghi chú, số lượng thẻ nhớ và thanh **`ProgressBar` chuẩn của WPF** biểu diễn tỷ lệ % thẻ *Đã thuộc*.
  - Tùy biến `ControlTemplate` cho `ProgressBar` trong `Resources/Templates.xaml` (bo tròn góc, hiển thị mượt mà).
  - Form Thêm mới / Sửa / Xóa Môn học (`Name`, `ColorHex`, `Description`) có kiểm tra **Validation** (không cho phép trùng tên hoặc để trống tên môn học).

---

## 3. Tiêu chí Nghiệm thu & Câu hỏi Vấn đáp cần nắm vững
1. **Giải thích cách tổ chức Layout (`DockPanel`, `Grid`, `WrapPanel`) giúp cửa sổ co giãn tốt (Tiêu chí 2)?**  
   $\rightarrow$ `DockPanel` cố định thanh điều hướng bên trái, phần nội dung chính tự lấp đầy không gian còn lại; `WrapPanel` giúp các thẻ thống kê tự xuống dòng khi chiều ngang hẹp; `Grid` dùng tỷ lệ `*` giúp chia cột linh hoạt.
2. **Sự khác nhau giữa `Style`, `DataTemplate` và `ControlTemplate` trong WPF?**  
   $\rightarrow$ `Style` tập hợp các thuộc tính giao diện (màu nền, padding, font); `ControlTemplate` thay đổi cấu trúc hình dáng bên trong của một Control (như bo góc `Button` hoặc `ProgressBar`); còn `DataTemplate` quy định cách hiển thị một đối tượng dữ liệu C# (`Subject`, `Flashcard`) lên danh sách.
