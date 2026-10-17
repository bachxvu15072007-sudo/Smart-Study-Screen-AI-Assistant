# 🤖 Kế Hoạch Chi Tiết — Thành Viên 2: Gemini AI & Quản Lý / Ôn Tập Flashcard (`Flashcard`)

## 1. Phạm vi Phụ trách
- **Bảng CSDL:** `Flashcard` (Thẻ ghi nhớ ôn tập: Câu hỏi, Đáp án, Mức độ thuộc `MasteryStatus`).
- **Màn hình (Views):**
  - `Views/FlashcardView.xaml` & `FlashcardView.xaml.cs`
- **ViewModel:** `ViewModels/FlashcardViewModel.cs`
- **Services & Helpers:**
  - `Services/Interfaces/IAiAssistantService.cs` & `Services/GeminiApiService.cs`
  - `Helpers/MasteryStatusToBrushConverter.cs`

---

## 2. Các Hạng mục Công việc Cụ thể (Checklist)

### Bước 1: Xây dựng `GeminiApiService` (Đa luồng HTTP & JSON Parsing)
- [ ] Cài đặt interface `IAiAssistantService` với 2 phương thức bất đồng bộ (`async/await`, hỗ trợ `CancellationToken`):
  1. `Task<string> ProcessTextAsync(string ocrText, string mode)`: Ghép prompt tương ứng với 1 trong 4 chế độ của `ComboBox` (*Dịch sang Tiếng Việt*, *Tóm tắt ý chính*, *Giải thích thuật ngữ/code*, *Tạo câu hỏi ôn tập*) và gửi `HttpClient.PostAsync` tới Google Gemini API (`gemini-2.5-flash` hoặc `gemini-1.5-flash`).
  2. `Task<List<Flashcard>> GenerateFlashcardsAsync(string ocrText, int subjectId)`: Yêu cầu Gemini trả về mảng JSON `[{"question": "...", "answer": "..."}]` và dùng `System.Text.Json` bóc tách thành danh sách đối tượng `Flashcard`.
- [ ] Bắt ngoại lệ đầy đủ (`HttpRequestException`, `TaskCanceledException`, `JsonException`) khi thiếu API Key, mất mạng hoặc hết hạn mức gọi API.

### Bước 2: Xây dựng `FlashcardView` & `FlashcardViewModel` (CRUD & Lọc dữ liệu)
- [ ] Hiển thị danh sách `Flashcard` theo Môn học (`Subject`) đang chọn và lọc theo mức độ thuộc (`Tất cả`, `Chưa thuộc`, `Đang học`, `Đã thuộc`).
- [ ] Form Thêm mới / Chỉnh sửa thẻ nhớ:
  - Chọn Môn học (`ComboBox`).
  - Nhập `Question` (Mặt trước) và `Answer` (Mặt sau).
  - **Validation:** Báo lỗi trên giao diện nếu để trống Câu hỏi hoặc Đáp án.
- [ ] Chức năng Xóa thẻ nhớ (có xác nhận trước khi xóa).

### Bước 3: Xây dựng Chế độ Lật thẻ Ôn tập (Flip Card Review Mode)
- [ ] Thiết kế vùng **Ôn tập nhanh** ngay trên `FlashcardView` sử dụng `DataTemplate` và cờ `[ObservableProperty] private bool _isAnswerVisible;`.
- [ ] Khi bấm vào thẻ (hoặc bấm nút **"🔄 Lật thẻ"**), chuyển đổi giữa hiển thị Câu hỏi (Mặt trước) và Đáp án (Mặt sau).
- [ ] Có 3 nút đánh giá mức độ thuộc bài:
  - 🔴 **Chưa thuộc (`MasteryStatus.New`)**
  - 🟡 **Đang học (`MasteryStatus.Learning`)**
  - 🟢 **Đã thuộc (`MasteryStatus.Mastered`)**
- [ ] Khi bấm 1 trong 3 nút, cập nhật trạng thái vào SQLite, tự động chuyển sang thẻ tiếp theo và cập nhật số liệu thống kê cho `DashboardView`.

---

## 3. Tiêu chí Nghiệm thu & Câu hỏi Vấn đáp cần nắm vững
1. **Tại sao phải dùng `async/await` khi gọi `GeminiApiService`? Nếu không dùng thì chuyện gì xảy ra với cửa sổ WPF?**  
   $\rightarrow$ Nếu gọi đồng bộ trên UI Thread (Dispatcher Thread), cửa sổ WPF sẽ bị treo ("Not Responding"), không thể bấm nút Hủy hay di chuyển cửa sổ trong lúc chờ phản hồi mạng 2–3 giây.
2. **Cách hoạt động của `MasteryStatusToBrushConverter` (`IValueConverter`) và `DataTemplate` trong `FlashcardView`?**  
   $\rightarrow$ Giải thích cách chuyển đổi giá trị Enum `MasteryStatus` trong ViewModel thành màu sắc (`SolidColorBrush` Đỏ / Vàng / Xanh lá) trên giao diện XAML mà không vi phạm MVVM.
