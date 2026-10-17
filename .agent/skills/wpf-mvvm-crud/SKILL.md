---
name: wpf-mvvm-crud
description: >-
  Guides the creation and modification of EF Core SQLite entities (Subject, SnippetNote, Flashcard),
  StudyRepository, and MVVM ViewModels with full CRUD, search/filter, and input validation.
  Use this skill whenever any team member asks to build or update database models, repository methods,
  or CRUD ViewModels in ScreenAIAssistant.
---

# Skill: Xây dựng CRUD Chuẩn MVVM & SQLite (`wpf-mvvm-crud`)

Sử dụng skill này khi xây dựng tầng dữ liệu (`Models`, `Data`) hoặc các chức năng Thêm – Xem – Sửa – Xóa – Tìm kiếm trong `ViewModels` của dự án `ScreenAIAssistant`.

## 1. Cấu trúc chuẩn của ViewModel CRUD với `CommunityToolkit.Mvvm`

Mọi ViewModel quản lý dữ liệu (`DashboardViewModel`, `StudyNotesViewModel`, `FlashcardViewModel`) phải tuân theo mẫu sau:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;

public partial class ExampleCrudViewModel : ViewModelBase
{
    private readonly IStudyRepository _repository;

    public ObservableCollection<Subject> Subjects { get; } = new();

    [ObservableProperty]
    private Subject? _selectedSubject;

    [ObservableProperty]
    private string _subjectNameInput = string.Empty;

    public ExampleCrudViewModel(IStudyRepository repository)
    {
        _repository = repository;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var items = await _repository.GetAllSubjectsAsync();
            Subjects.Clear();
            foreach (var item in items)
                Subjects.Add(item);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveSubjectAsync()
    {
        // Bắt buộc kiểm tra nhập liệu sai (Tiêu chí 1)
        if (string.IsNullOrWhiteSpace(SubjectNameInput))
        {
            ErrorMessage = "Vui lòng nhập tên môn học!";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            // Thực hiện lưu qua Repository và đồng bộ lại ObservableCollection
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## 2. Quy trình Kiểm tra (Verification Checklist)
1. Đảm bảo file CSDL SQLite được lưu tại `%LocalAppData%\ScreenAIAssistant\study_assistant.db`.
2. Gọi `dbContext.Database.EnsureCreated()` trong `App.xaml.cs` khi khởi động ứng dụng kèm dữ liệu mẫu (Seed Data) nếu bảng đang trống.
3. Chạy `dotnet build` trong thư mục `ScreenAIAssistant\ScreenAIAssistant` để xác nhận không có lỗi biên dịch.
