# 🗺️ Master Roadmap & Quy Ước Phối Hợp Nhóm 3 Thành Viên

Tài liệu này là kế hoạch tổng thể kết nối công việc của 3 thành viên trong dự án **Smart Study & Screen AI Assistant (`ScreenAIAssistant`)**.

---

## 1. Phân chia Trách nhiệm Độc lập (3 Bảng = 3 Màn hình = 3 Thành viên)

Để không dẫm chân lên nhau khi code chung một Solution WPF, dự án được chia theo ranh giới file rõ ràng:

| Thành viên | Nhánh Git chính | Bảng CSDL phụ trách | Màn hình & Dịch vụ sở hữu riêng | Kế hoạch chi tiết |
| :--- | :--- | :--- | :--- | :--- |
| **Thành viên 1** | `feature/m1-ocr-notes` | `SnippetNote` | `OverlayWindow`, `StudyNotesView`, `StudyNotesViewModel`, `ScreenCaptureService`, `WindowsOcrService`, `DpiHelper` | [01-member1-ocr-studynotes.md](./01-member1-ocr-studynotes.md) |
| **Thành viên 2** | `feature/m2-ai-flashcards` | `Flashcard` | `FlashcardView`, `FlashcardViewModel`, `GeminiApiService`, `MasteryStatusToBrushConverter` | [02-member2-ai-flashcards.md](./02-member2-ai-flashcards.md) |
| **Thành viên 3** | `feature/m3-db-dashboard` | `Subject` | `AppDbContext`, `StudyRepository`, `MainWindow`, `MainViewModel`, `DashboardView`, `DashboardViewModel`, `Resources/*.xaml` | [03-member3-db-dashboard-styles.md](./03-member3-db-dashboard-styles.md) |

---

## 2. Hợp đồng Giao tiếp Chung (Interfaces & Shared Models)

Trong **Tuần 1**, cả nhóm thống nhất khóa cứng (freeze) các file dùng chung sau đây trên nhánh `develop` trước khi tách nhánh làm việc riêng:

1. **Models (`Models/`):**
   - `Subject.cs`: `Id`, `Name`, `ColorHex`, `Description`, `CreatedAt`, `ICollection<SnippetNote> Notes`, `ICollection<Flashcard> Flashcards`
   - `SnippetNote.cs`: `Id`, `SubjectId`, `Title`, `ImagePath`, `OcrText`, `AiResponse`, `PersonalNote`, `IsFavorite`, `CreatedAt`
   - `Flashcard.cs`: `Id`, `SubjectId`, `SnippetNoteId`, `Question`, `Answer`, `MasteryStatus` (`0: Chưa thuộc`, `1: Đang học`, `2: Đã thuộc`), `LastReviewedAt`
2. **Interfaces (`Services/Interfaces/` & `Data/`):**
   - `IStudyRepository.cs`: Chứa các phương thức CRUD bất đồng bộ cho cả 3 bảng `Subject`, `SnippetNote`, `Flashcard`.
   - `IScreenCaptureService.cs` & `IOcrService.cs`: Do Thành viên 1 cài đặt.
   - `IAiAssistantService.cs`: Do Thành viên 2 cài đặt (để cả `StudyNotesViewModel` của TV1 và `FlashcardViewModel` của TV2 cùng gọi được qua Dependency Injection).

---

## 3. Lộ trình 5 Tuần Đồng bộ

- **Tuần 1 (Khởi tạo):** Thành viên 3 tạo khung Solution, 3 Models, `AppDbContext` và `IStudyRepository`, đẩy lên `develop`. Thành viên 1 & 2 pull nhánh `develop` về để bắt đầu viết Services.
- **Tuần 2 (Services lõi):**
  - TV1 hoàn thành `OverlayWindow` + `ScreenCaptureService` + `WindowsOcrService`.
  - TV2 hoàn thành `GeminiApiService` (xử lý 4 chế độ `ComboBox` & sinh JSON `Flashcard`).
  - TV3 hoàn thành `MainWindow` điều hướng và CRUD `Subject` trên `DashboardView`.
- **Tuần 3 (Màn hình CRUD & Tích hợp):**
  - TV1 hoàn thành `StudyNotesView` (Master-Detail CRUD `SnippetNote`).
  - TV2 hoàn thành `FlashcardView` (CRUD `Flashcard` + Chế độ Lật thẻ ôn tập).
  - TV3 hoàn thành thống kê 3 thẻ số liệu lớn + thanh `ProgressBar` trên `DashboardView`.
- **Tuần 4 (UI Polish & Validation):** Hoàn thiện `Styles.xaml`, `Templates.xaml`, kiểm tra co giãn cửa sổ và bắt lỗi nhập liệu sai trên cả 3 màn hình.
- **Tuần 5 (Nghiệm thu & Bảo vệ):** Merge toàn bộ vào `main`, tạo dữ liệu mẫu và chạy thử kịch bản Demo 5 phút.
