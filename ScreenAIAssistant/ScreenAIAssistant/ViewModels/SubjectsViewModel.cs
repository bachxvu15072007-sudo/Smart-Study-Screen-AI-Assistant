using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel Tab 2 — MÔN HỌC.
/// Danh sách môn học (CRUD + Tìm kiếm). Click vào một môn → mở Không gian Học tập của môn đó.
/// </summary>
public partial class SubjectsViewModel : ViewModelBase
{
    private readonly IStudyRepository _repository;
    private List<SubjectProgress> _allSubjectsCache = new();

    /// <summary>
    /// Sự kiện yêu cầu MainViewModel mở Không gian Học tập cho môn được chọn.
    /// </summary>
    public event Func<Subject, Task>? OpenWorkspaceRequested;

    public ObservableCollection<SubjectProgress> SubjectList { get; } = new();

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private bool _hasSubjects;

    #region Form Thêm / Sửa Môn học (khung nổi phía trên danh sách)

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _nameInput = string.Empty;

    [ObservableProperty]
    private string _colorInput = "#2F6FEB";

    [ObservableProperty]
    private string _descriptionInput = string.Empty;

    private Subject? _editingSubject;

    /// <summary>
    /// Bảng màu dịu mắt gợi ý cho môn học.
    /// </summary>
    public List<string> ColorPalette { get; } = new()
    {
        "#2F6FEB", // Xanh dương tập trung
        "#0F9D8F", // Xanh ngọc
        "#7C5CDB", // Tím oải hương
        "#D97706", // Hổ phách
        "#E11D48", // Hồng đậm
        "#0891B2", // Xanh lơ
        "#16A34A", // Xanh lá
        "#64748B"  // Xám đá
    };

    #endregion

    public SubjectsViewModel(IStudyRepository repository)
    {
        _repository = repository;
    }

    partial void OnSearchKeywordChanged(string value) => ApplySearchFilter();

    private void ApplySearchFilter()
    {
        SubjectList.Clear();
        var query = _allSubjectsCache.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            string kw = SearchKeyword.Trim().ToLower();
            query = query.Where(s => s.Subject.Name.ToLower().Contains(kw)
                                  || s.Subject.Description.ToLower().Contains(kw));
        }

        foreach (var item in query)
        {
            SubjectList.Add(item);
        }

        HasSubjects = SubjectList.Count > 0;
    }

    [RelayCommand]
    public async Task LoadSubjectsAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            _allSubjectsCache = await _repository.GetSubjectProgressListAsync();
            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách môn học: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Click vào thẻ môn học → mở Không gian Học tập.
    /// </summary>
    [RelayCommand]
    public async Task OpenSubjectAsync(SubjectProgress? item)
    {
        if (item == null || OpenWorkspaceRequested == null)
        {
            return;
        }

        await OpenWorkspaceRequested.Invoke(item.Subject);
    }

    [RelayCommand]
    public void ShowAddForm()
    {
        _editingSubject = null;
        NameInput = string.Empty;
        ColorInput = "#2F6FEB";
        DescriptionInput = string.Empty;
        IsEditing = false;
        IsFormOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void ShowEditForm(SubjectProgress? item)
    {
        if (item == null)
        {
            return;
        }

        _editingSubject = item.Subject;
        NameInput = item.Subject.Name;
        ColorInput = string.IsNullOrWhiteSpace(item.Subject.ColorHex) ? "#2F6FEB" : item.Subject.ColorHex;
        DescriptionInput = item.Subject.Description;
        IsEditing = true;
        IsFormOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        _editingSubject = null;
        ClearError();
    }

    [RelayCommand]
    public void SelectColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            ColorInput = colorHex;
        }
    }

    /// <summary>
    /// Lưu môn học (Thêm mới hoặc Cập nhật) với Validation chống rỗng & trùng tên.
    /// </summary>
    [RelayCommand]
    public async Task SaveSubjectAsync()
    {
        ClearError();

        if (string.IsNullOrWhiteSpace(NameInput))
        {
            ErrorMessage = "Vui lòng nhập tên môn học!";
            return;
        }

        string trimmedName = NameInput.Trim();

        try
        {
            IsBusy = true;

            bool isDuplicate = await _repository.IsSubjectNameExistsAsync(trimmedName, _editingSubject?.Id);
            if (isDuplicate)
            {
                ErrorMessage = $"Tên môn học \"{trimmedName}\" đã tồn tại!";
                return;
            }

            if (IsEditing && _editingSubject != null)
            {
                _editingSubject.Name = trimmedName;
                _editingSubject.ColorHex = ColorInput;
                _editingSubject.Description = DescriptionInput?.Trim() ?? string.Empty;
                await _repository.UpdateSubjectAsync(_editingSubject);
                StatusMessage = $"Đã cập nhật môn \"{trimmedName}\".";
            }
            else
            {
                await _repository.AddSubjectAsync(new Subject
                {
                    Name = trimmedName,
                    ColorHex = ColorInput,
                    Description = DescriptionInput?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.Now
                });
                StatusMessage = $"Đã thêm môn \"{trimmedName}\".";
            }

            IsFormOpen = false;
            _editingSubject = null;
            await LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lưu môn học: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Xóa môn học (có hộp thoại xác nhận).
    /// </summary>
    [RelayCommand]
    public async Task DeleteSubjectAsync(SubjectProgress? item)
    {
        if (item == null)
        {
            return;
        }

        string name = item.Subject.Name;

        if (Application.Current?.MainWindow is { IsLoaded: true })
        {
            var confirm = MessageBox.Show(
                $"Xóa môn học \"{name}\" cùng toàn bộ tài liệu, ghi chú và flashcard liên quan?",
                "Xác nhận xóa môn học",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _repository.DeleteSubjectAsync(item.Subject.Id);
            StatusMessage = $"Đã xóa môn \"{name}\".";
            await LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xóa môn học: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
