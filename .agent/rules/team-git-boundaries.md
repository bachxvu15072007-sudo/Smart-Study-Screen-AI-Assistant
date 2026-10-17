# Quy Tắc Phân Vùng Mã Nguồn Nhóm 3 Thành Viên (`team-git-boundaries`)

Để đảm bảo điểm **Teamwork (0.5 điểm)** và tránh xung đột code (Merge Conflict) khi 3 thành viên cùng làm việc trên Git:

## 1. Ranh giới Sở hữu File của Từng Thành viên

- **Thành viên 1 (`feature/m1-ocr-notes`):**
  - Sở hữu: `Views/OverlayWindow.*`, `Views/StudyNotesView.*`, `ViewModels/StudyNotesViewModel.cs`, `Services/ScreenCaptureService.cs`, `Services/WindowsOcrService.cs`, `Helpers/DpiHelper.cs`, `Helpers/ImagePathToBitmapConverter.cs`.
- **Thành viên 2 (`feature/m2-ai-flashcards`):**
  - Sở hữu: `Views/FlashcardView.*`, `ViewModels/FlashcardViewModel.cs`, `Services/GeminiApiService.cs`, `Helpers/MasteryStatusToBrushConverter.cs`.
- **Thành viên 3 (`feature/m3-db-dashboard`):**
  - Sở hữu: `Models/*`, `Data/*`, `Views/MainWindow.*`, `Views/DashboardView.*`, `ViewModels/MainViewModel.cs`, `ViewModels/DashboardViewModel.cs`, `Resources/*.xaml`, `App.xaml.*`.

## 2. Quy tắc khi Cần Sửa File Dùng Chung
- Các file dùng chung gồm: `ScreenAIAssistant.csproj`, `App.xaml.cs`, `Data/IStudyRepository.cs`, `Data/StudyRepository.cs`, `Resources/Styles.xaml`.
- Nếu Thành viên 1 hoặc Thành viên 2 cần thêm phương thức mới vào `IStudyRepository` hoặc thêm Style mới vào `Resources/Styles.xaml`:
  1. Chỉ thêm phương thức/resource mới ở cuối file, không tự ý đổi tên hoặc xóa các phương thức/resource mà thành viên khác đang sử dụng.
  2. Luôn giữ cho Solution biên dịch thành công (`dotnet build` đạt 0 Error) trước khi commit.
