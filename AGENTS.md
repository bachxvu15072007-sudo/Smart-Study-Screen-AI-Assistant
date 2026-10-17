# 🤖 AGENTS.md — Quy Tắc Chung Cho AI Agent Trong Dự Án `ScreenAIAssistant`

Tài liệu này được tự động nạp cho mọi thành viên trong nhóm khi làm việc với AI Agent trên dự án **Smart Study & Screen AI Assistant (`ScreenAIAssistant`)** — Đồ án cuối kỳ môn **Lập Trình Trực Quan (IT008)**.

---

## 1. Bối cảnh & Mục tiêu Dự án
- **Đề tài:** Ứng dụng Quản lý Học tập & Ghi chú Thông minh tích hợp OCR và AI (`ScreenAIAssistant`).
- **Nền tảng:** WPF trên `.NET 10` (`net10.0-windows10.0.19041.0`), kiến trúc **MVVM** (`CommunityToolkit.Mvvm`), CSDL **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`).
- **Cấu trúc 3 Bảng = 3 Màn hình = 3 Thành viên:**
  1. **Thành viên 1:** Bảng `SnippetNote` + `StudyNotesView` + `OverlayWindow` + `ScreenCaptureService` + `WindowsOcrService` ([Kế hoạch](./plans/01-member1-ocr-studynotes.md)).
  2. **Thành viên 2:** Bảng `Flashcard` + `FlashcardView` + `GeminiApiService` ([Kế hoạch](./plans/02-member2-ai-flashcards.md)).
  3. **Thành viên 3:** Bảng `Subject` + `DashboardView` + `MainWindow` + `AppDbContext` + `Resources/*.xaml` ([Kế hoạch](./plans/03-member3-db-dashboard-styles.md)).

---

## 2. Hệ thống Rules & Skills trong `.agent/`

Khi hỗ trợ bất kỳ thành viên nào viết code, Agent **BẮT BUỘC** tuân thủ các quy tắc trong `.agent/rules/` và kích hoạt đúng Skill trong `.agent/skills/`:

### 📜 Danh sách Rules (`.agent/rules/`)
- @[WPF MVVM Strict Rules](./.agent/rules/wpf-mvvm-strict.md) — Quy tắc bắt buộc về MVVM, Data Binding, Command và xử lý bất đồng bộ.
- @[IT008 Grading Rubric Rules](./.agent/rules/it008-grading-rubric.md) — Quy tắc đảm bảo mọi màn hình đều đạt điểm tối đa theo thang chấm điểm IT008 (CRUD, Validation, Layout co giãn, Style/Template).
- @[Team Git Boundaries](./.agent/rules/team-git-boundaries.md) — Quy tắc phân vùng file giữa 3 thành viên để chống xung đột Git (Merge Conflict).

### 🛠️ Danh sách Skills chuyên biệt (`.agent/skills/`)
- `wpf-mvvm-crud`: Sử dụng khi tạo hoặc chỉnh sửa Models, `AppDbContext`, `StudyRepository`, ViewModels CRUD và Validation.
- `screen-capture-ocr`: Sử dụng khi làm việc với `OverlayWindow`, vẽ `Canvas` / `CombinedGeometry`, quy đổi DPI (`DpiHelper`) và gọi `Windows.Media.Ocr`.
- `gemini-flashcard-ai`: Sử dụng khi làm việc với `GeminiApiService` (`HttpClient`), xử lý 4 chế độ `ComboBox` và sinh JSON `Flashcard` tự động.
- `wpf-ui-styles-templates`: Sử dụng khi thiết kế XAML Layout (`DockPanel`, `GridSplitter`, `WrapPanel`), `ResourceDictionary`, `ControlTemplate` (`ProgressBar`) và `DataTemplate` (Flip Card).
