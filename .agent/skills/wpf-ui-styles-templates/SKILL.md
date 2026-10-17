---
name: wpf-ui-styles-templates
description: >-
  Guides the creation of WPF Layouts (DockPanel, GridSplitter, WrapPanel), ResourceDictionaries
  (Colors.xaml, Styles.xaml, Templates.xaml), custom ControlTemplates (rounded ProgressBar, Buttons),
  and DataTemplates (Flip Card, Subject statistics card).
  Use this skill when working on Member 3 tasks or styling any XAML View in ScreenAIAssistant.
---

# Skill: Thiết Kế Layout Co Giãn, Styles & ControlTemplate Chuẩn WPF (`wpf-ui-styles-templates`)

Sử dụng skill này khi xây dựng giao diện XAML và bộ `ResourceDictionary` để đạt điểm tuyệt đối ở **Tiêu chí 2 (Giao diện - 2.0đ)** và **Tiêu chí 3' (Template - 3.0đ)**.

## 1. Bố cục Chuẩn cho 3 Màn hình Chính
1. **`MainWindow.xaml` (`DockPanel`):**
   - `MinWidth="960" MinHeight="620"`.
   - Bên trái (`DockPanel.Dock="Left"`, `Width="240"`): Sidebar chứa tên ứng dụng, 3 nút chuyển màn hình (`Dashboard`, `Ghi chú OCR`, `Flashcard`) và ô nhập `Gemini API Key`.
   - Phần còn lại: `ContentControl` hiển thị màn hình đang chọn.
2. **`DashboardView.xaml` (`WrapPanel` + `Grid`):**
   - Phía trên: `WrapPanel` chứa **3 thẻ số liệu lớn** (Tổng số Môn học, Tổng số Ghi chú, Tổng số Thẻ Flashcard). Khi kéo thu nhỏ cửa sổ, các thẻ tự động rớt dòng mượt mà.
   - Phía dưới: `Grid` 2 cột — Cột trái là danh sách Môn học kèm thanh **`ProgressBar`** tỷ lệ thuộc bài; Cột phải là Form Thêm/Sửa/Xóa Môn học (`Subject`).
3. **`StudyNotesView.xaml` & `FlashcardView.xaml` (`Grid` + `GridSplitter`):**
   - Chia 2 cột chính bằng `Grid.ColumnDefinitions` (`Width="320"` và `Width="*"`), ở giữa đặt `<GridSplitter Width="5" HorizontalAlignment="Stretch" />` để giảng viên có thể kéo giãn cột trực tiếp khi chấm bài.

## 2. Tùy biến `ControlTemplate` cho Thanh `ProgressBar` (Tiêu chí 3')
Trong `Resources/Templates.xaml`, định nghĩa `Style` kèm `ControlTemplate` cho `ProgressBar` sử dụng `Border` có `CornerRadius="6"` và phần `PART_Track` / `PART_Indicator` bo tròn để biểu diễn phần trăm Flashcard đã thuộc của từng môn học mà không cần cài thêm thư viện biểu đồ ngoài.

## 3. Đăng ký `ResourceDictionary` trong `App.xaml`
Luôn gộp cả 3 file tài nguyên trong `App.xaml`:
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Colors.xaml" />
            <ResourceDictionary Source="Resources/Styles.xaml" />
            <ResourceDictionary Source="Resources/Templates.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```
