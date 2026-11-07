using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.ViewModels;

/// <summary>
/// ViewModel quản lý màn hình Tổng quan & Môn học (DashboardView).
/// Xử lý thống kê 3 thẻ lớn, thanh ProgressBar tiến độ từng môn học và CRUD + Tìm kiếm bảng Subject.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStudyRepository _repository;
    private List<SubjectProgress> _allSubjectsCache = new();

    #region 1. Dữ liệu Thống kê & Danh sách Môn học

    [ObservableProperty]
    private int _totalSubjects;

    [ObservableProperty]
    private int _totalNotes;

    [ObservableProperty]
    private int _totalFlashcards;

    [ObservableProperty]
    private int _masteredFlashcards;

    [ObservableProperty]
    private double _overallMasteryRate;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// Danh sách môn học kèm dữ liệu tiến độ để binding lên ListBox/ItemsControl.
    /// </summary>
    public ObservableCollection<SubjectProgress> SubjectList { get; } = new();

    #endregion

    #region 2. Thuộc tính Form CRUD Môn học & Bảng màu

    [ObservableProperty]
    private SubjectProgress? _selectedSubjectProgress;

    [ObservableProperty]
    private string _nameInput = string.Empty;

    [ObservableProperty]
    private string _colorInput = "#2563EB";

    [ObservableProperty]
    private string _descriptionInput = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Bảng màu gợi ý để người dùng chọn nhanh cho Môn học.
    /// </summary>
    public List<string> ColorPalette { get; } = new()
    {
        "#2563EB", // Blue
        "#059669", // Emerald
        "#7C3AED", // Violet
        "#D97706", // Amber
        "#DC2626", // Red
        "#0891B2"  // Cyan
    };

    #endregion

    public DashboardViewModel(IStudyRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Tự động lọc danh sách môn học khi người dùng gõ từ khóa tìm kiếm.
    /// </summary>
    partial void OnSearchKeywordChanged(string value)
    {
        ApplySearchFilter();
    }

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
    }

    /// <summary>
    /// Tự động điền dữ liệu vào Form khi người dùng chọn một môn học trong danh sách.
    /// </summary>
    partial void OnSelectedSubjectProgressChanged(SubjectProgress? value)
    {
        if (value != null)
        {
            NameInput = value.Subject.Name;
            ColorInput = string.IsNullOrWhiteSpace(value.Subject.ColorHex) ? "#2563EB" : value.Subject.ColorHex;
            DescriptionInput = value.Subject.Description;
            IsEditing = true;
            ClearError();
        }
    }

    #region 3. Các Relay Commands

    /// <summary>
    /// Tải toàn bộ dữ liệu thống kê và danh sách môn học từ CSDL SQLite bất đồng bộ.
    /// </summary>
    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            TotalSubjects = await _repository.GetTotalSubjectsCountAsync();
            TotalNotes = await _repository.GetTotalNotesCountAsync();
            TotalFlashcards = await _repository.GetTotalFlashcardsCountAsync();
            MasteredFlashcards = await _repository.GetMasteredFlashcardsCountAsync();

            OverallMasteryRate = TotalFlashcards > 0
                ? Math.Round((double)MasteredFlashcards * 100.0 / TotalFlashcards, 1)
                : 0.0;

            _allSubjectsCache = await _repository.GetSubjectProgressListAsync();
            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải dữ liệu Dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Thêm mới hoặc Cập nhật Môn học (có kiểm tra Validation chống rỗng và chống trùng tên).
    /// </summary>
    [RelayCommand]
    public async Task SaveSubjectAsync()
    {
        ClearError();

        // 1. Validation kiểm tra rỗng (Tiêu chí 1)
        if (string.IsNullOrWhiteSpace(NameInput))
        {
            ErrorMessage = "Vui lòng nhập tên môn học!";
            return;
        }

        string trimmedName = NameInput.Trim();

        try
        {
            IsBusy = true;

            // 2. Validation kiểm tra trùng lặp tên môn học (Tiêu chí 1)
            int? excludeId = IsEditing ? SelectedSubjectProgress?.Subject.Id : null;
            bool isDuplicate = await _repository.IsSubjectNameExistsAsync(trimmedName, excludeId);
            if (isDuplicate)
            {
                ErrorMessage = $"Tên môn học \"{trimmedName}\" đã tồn tại, vui lòng chọn tên khác!";
                return;
            }

            if (IsEditing && SelectedSubjectProgress != null)
            {
                // Cập nhật môn học đã có
                var subject = SelectedSubjectProgress.Subject;
                subject.Name = trimmedName;
                subject.ColorHex = ColorInput;
                subject.Description = DescriptionInput?.Trim() ?? string.Empty;

                await _repository.UpdateSubjectAsync(subject);
                StatusMessage = $"Đã cập nhật môn học \"{trimmedName}\" thành công!";
            }
            else
            {
                // Thêm môn học mới
                var newSubject = new Subject
                {
                    Name = trimmedName,
                    ColorHex = ColorInput,
                    Description = DescriptionInput?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                await _repository.AddSubjectAsync(newSubject);
                StatusMessage = $"Đã thêm môn học mới \"{trimmedName}\" thành công!";
            }

            ResetForm();
            await LoadDashboardDataAsync();
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
    /// Xóa môn học đang chọn (có hộp thoại xác nhận khi chạy trên giao diện - Tiêu chí 1).
    /// </summary>
    [RelayCommand]
    public async Task DeleteSubjectAsync()
    {
        if (SelectedSubjectProgress == null)
        {
            ErrorMessage = "Vui lòng chọn môn học cần xóa!";
            return;
        }

        string subjectName = SelectedSubjectProgress.Subject.Name;

        // Hiển thị hộp thoại xác nhận nếu đang chạy trong môi trường cửa sổ WPF
        if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
        {
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa môn học \"{subjectName}\" cùng toàn bộ ghi chú và thẻ Flashcard liên quan không?",
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

            int subjectId = SelectedSubjectProgress.Subject.Id;

            await _repository.DeleteSubjectAsync(subjectId);
            StatusMessage = $"Đã xóa môn học \"{subjectName}\" thành công!";

            ResetForm();
            await LoadDashboardDataAsync();
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

    /// <summary>
    /// Làm mới Form nhập liệu về trạng thái ban đầu.
    /// </summary>
    [RelayCommand]
    public void ResetForm()
    {
        SelectedSubjectProgress = null;
        NameInput = string.Empty;
        ColorInput = "#2563EB";
        DescriptionInput = string.Empty;
        IsEditing = false;
        ClearError();
    }

    /// <summary>
    /// Chọn màu sắc nhanh từ ColorPalette.
    /// </summary>
    [RelayCommand]
    public void SelectColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            ColorInput = colorHex;
        }
    }

    #endregion
}
