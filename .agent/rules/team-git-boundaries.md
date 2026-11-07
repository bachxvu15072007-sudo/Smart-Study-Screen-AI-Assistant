# Quy Tắc Phân Vùng Mã Nguồn & Quy Trình Git Nhóm 3 Thành Viên (`team-git-boundaries`)

Tài liệu này quy định ranh giới sở hữu file và quy trình làm việc nhóm trên Git nhằm đảm bảo đạt điểm tối đa ở **Tiêu chí 5 - Teamwork (0.5 điểm)** và tránh xung đột mã nguồn (Merge Conflict):

---

## 1. Ranh giới Sở hữu File của Từng Thành viên

- **Thành viên 1 (`feature/m1-ocr-notes`):**
  - Sở hữu: `Views/OverlayWindow.*`, `Views/StudyNotesView.*`, `ViewModels/StudyNotesViewModel.cs`, `Services/ScreenCaptureService.cs`, `Services/WindowsOcrService.cs`, `Helpers/DpiHelper.cs`, `Helpers/ImagePathToBitmapConverter.cs`.
- **Thành viên 2 (`feature/m2-ai-flashcards`):**
  - Sở hữu: `Views/FlashcardView.*`, `ViewModels/FlashcardViewModel.cs`, `Services/GeminiApiService.cs`, `Helpers/MasteryStatusToBrushConverter.cs`.
- **Thành viên 3 (`feature/m3-db-dashboard`):**
  - Sở hữu: `Models/*`, `Data/*`, `Views/MainWindow.*`, `Views/DashboardView.*`, `ViewModels/MainViewModel.cs`, `ViewModels/DashboardViewModel.cs`, `Resources/*.xaml`, `App.xaml.*`.

---

## 2. Quy tắc khi Cần Sửa File Dùng Chung

- Các file dùng chung gồm: `ScreenAIAssistant.csproj`, `App.xaml.cs`, `Data/IStudyRepository.cs`, `Data/StudyRepository.cs`, `Resources/Styles.xaml`.
- Nếu Thành viên 1 hoặc Thành viên 2 cần thêm phương thức mới vào `IStudyRepository` hoặc thêm Style mới vào `Resources/Styles.xaml`:
  1. Chỉ thêm phương thức/resource mới ở cuối file, không tự ý đổi tên hoặc xóa các phương thức/resource mà thành viên khác đang sử dụng.
  2. Luôn giữ cho Solution biên dịch thành công (`dotnet build` đạt 0 Error) trước khi commit.

---

## 3. Quy trình Phối hợp Phân nhánh Git Flow Chuẩn (Branching, PR & Merge)

Mọi thành viên và AI Agent khi làm việc trong dự án bắt buộc tuân thủ mô hình phân nhánh sau:

```
feature/m1-ocr-notes   ──┐
feature/m2-ai-flashcards ──┼──> Pull Request ──> develop ──> (Nghiệm thu hoàn tất) ──> Pull Request ──> main
feature/m3-db-dashboard  ──┘
```

### A. Vai trò 3 Cấp Nhánh:
1. **Nhánh `main` (Nhánh sản phẩm cuối):**
   - Chỉ chứa mã nguồn hoàn chỉnh đã qua kiểm thử nghiệm thu (Final Release để nộp bài và chấm điểm).
   - **TUYỆT ĐỐI KHÔNG** commit hay push code làm việc hàng ngày trực tiếp vào `main`.
2. **Nhánh `develop` (Nhánh tích hợp sống dài hạn):**
   - Là nhánh mẹ của toàn bộ dự án, tồn tại xuyên suốt từ Tuần 1 đến Tuần 5.
   - Mọi nhánh tính năng của các thành viên đều được tách ra từ `develop` và tạo Pull Request gộp về lại `develop`.
   - **TUYỆT ĐỐI KHÔNG ĐƯỢC XÓA NHÁNH `develop`**.
3. **Nhánh tính năng `feature/*` (Nhánh làm việc của từng thành viên):**
   - Thành viên 1: `feature/m1-ocr-notes`
   - Thành viên 2: `feature/m2-ai-flashcards`
   - Thành viên 3: `feature/m3-db-dashboard`

### B. Vòng Đời Phát Triển Một Tính Năng:
1. **Tách nhánh:** Trước khi code, kéo `develop` mới nhất về và tạo nhánh tính năng riêng:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/m...
   ```
2. **Phát triển & Kiểm thử:** Viết code trong đúng ranh giới file của mình, chạy `dotnet build` đạt 0 lỗi trước khi commit.
3. **Đẩy lên GitHub:**
   ```bash
   git push -u origin feature/m...
   ```
4. **Tạo Pull Request:** Mở Pull Request trên GitHub với đích đến là `develop`:
   - `base: develop` $\leftarrow$ `compare: feature/m...`
5. **Merge & Dọn dẹp:** Sau khi nhóm review và bấm **Merge Pull Request** vào `develop`, có thể xóa nhánh `feature` đó trên GitHub để dọn dẹp kho lưu trữ.
6. **Nghiệm thu Đồ án (Release to Main):** Khi hoàn thành trọn vẹn cả 3 màn hình và kiểm thử tích hợp (Tuần 5), nhóm tạo Pull Request từ `develop` vào `main` để đem đi bảo vệ.
