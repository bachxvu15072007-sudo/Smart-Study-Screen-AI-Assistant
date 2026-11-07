using System.IO;
using Microsoft.EntityFrameworkCore;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.Data;

/// <summary>
/// DbContext quản lý kết nối CSDL SQLite cho ứng dụng ScreenAIAssistant.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SnippetNote> SnippetNotes => Set<SnippetNote>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    public string DbPath { get; }

    public AppDbContext()
    {
        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenAIAssistant");

        Directory.CreateDirectory(localFolder);
        DbPath = Path.Combine(localFolder, "study_assistant.db");
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenAIAssistant");

        Directory.CreateDirectory(localFolder);
        DbPath = Path.Combine(localFolder, "study_assistant.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình Subject (1 - N) SnippetNote
        modelBuilder.Entity<Subject>()
            .HasMany(s => s.Notes)
            .WithOne(n => n.Subject)
            .HasForeignKey(n => n.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình Subject (1 - N) Flashcard
        modelBuilder.Entity<Subject>()
            .HasMany(s => s.Flashcards)
            .WithOne(f => f.Subject)
            .HasForeignKey(f => f.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình SnippetNote (1 - N) Flashcard (Nullable FK)
        modelBuilder.Entity<SnippetNote>()
            .HasMany(n => n.Flashcards)
            .WithOne(f => f.SnippetNote)
            .HasForeignKey(f => f.SnippetNoteId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// Nạp dữ liệu mẫu ban đầu (Seed Data) nếu cơ sở dữ liệu còn trống để phục vụ chấm điểm và Demo.
    /// </summary>
    public void SeedInitialData()
    {
        if (Subjects.Any()) return;

        var sub1 = new Subject
        {
            Name = "Lập trình trực quan (IT008)",
            ColorHex = "#2563EB",
            Description = "Lập trình ứng dụng WPF trên .NET, MVVM, XAML, EF Core SQLite",
            CreatedAt = DateTime.Now.AddDays(-10)
        };

        var sub2 = new Subject
        {
            Name = "Cấu trúc dữ liệu & Giải thuật",
            ColorHex = "#059669",
            Description = "Cây nhị phân, Đồ thị, Bảng băm, Thuật toán sắp xếp và tìm kiếm",
            CreatedAt = DateTime.Now.AddDays(-8)
        };

        Subjects.AddRange(sub1, sub2);
        SaveChanges();

        var note1 = new SnippetNote
        {
            SubjectId = sub1.Id,
            Title = "Kiến trúc MVVM trong WPF",
            ImagePath = string.Empty,
            OcrText = "Model-View-ViewModel (MVVM) is an architectural pattern that facilitates the separation of GUI development from business logic.",
            AiResponse = "MVVM là kiến trúc gồm Model (Dữ liệu), View (Giao diện XAML) và ViewModel (Điều phối dữ liệu qua Data Binding), giúp code dễ bảo trì.",
            PersonalNote = "Quy tắc cốt lõi: Không viết logic nghiệp vụ trong code-behind (.xaml.cs).",
            IsFavorite = true,
            CreatedAt = DateTime.Now.AddDays(-5)
        };

        var note2 = new SnippetNote
        {
            SubjectId = sub2.Id,
            Title = "Độ phức tạp QuickSort",
            ImagePath = string.Empty,
            OcrText = "QuickSort average time complexity is O(n log n). In the worst case, it can degrade to O(n^2).",
            AiResponse = "Thuật toán QuickSort chia để trị, trung bình O(n log n). Trường hợp xấu nhất O(n^2) khi pivot không tối ưu.",
            PersonalNote = "Nên dùng kỹ thuật chọn pivot ngẫu nhiên (Randomized Pivot).",
            IsFavorite = false,
            CreatedAt = DateTime.Now.AddDays(-3)
        };

        SnippetNotes.AddRange(note1, note2);
        SaveChanges();

        var cards = new List<Flashcard>
        {
            new()
            {
                SubjectId = sub1.Id,
                SnippetNoteId = note1.Id,
                Question = "MVVM là viết tắt của gì và gồm những thành phần nào?",
                Answer = "Model - View - ViewModel. Model: dữ liệu nghiệp vụ, View: giao diện XAML, ViewModel: trung gian kết nối qua Data Binding và Command.",
                MasteryStatus = MasteryStatus.Mastered,
                LastReviewedAt = DateTime.Now.AddDays(-1)
            },
            new()
            {
                SubjectId = sub1.Id,
                SnippetNoteId = note1.Id,
                Question = "Tại sao nên dùng ObservableCollection thay vì List trong WPF ViewModel?",
                Answer = "Vì ObservableCollection tự động phát sinh sự kiện CollectionChanged khi thêm/xóa, giúp UI tự động đồng bộ mà không cần refresh thủ công.",
                MasteryStatus = MasteryStatus.Learning,
                LastReviewedAt = DateTime.Now.AddHours(-12)
            },
            new()
            {
                SubjectId = sub2.Id,
                SnippetNoteId = note2.Id,
                Question = "Độ phức tạp thời gian trung bình của QuickSort là bao nhiêu?",
                Answer = "O(n log n).",
                MasteryStatus = MasteryStatus.Mastered,
                LastReviewedAt = DateTime.Now.AddDays(-2)
            },
            new()
            {
                SubjectId = sub2.Id,
                SnippetNoteId = note2.Id,
                Question = "Trường hợp xấu nhất của QuickSort có độ phức tạp là bao nhiêu và khi nào xảy ra?",
                Answer = "O(n^2), xảy ra khi dãy đã có thứ tự và pivot luôn chọn ở vị trí đầu hoặc cuối mảng.",
                MasteryStatus = MasteryStatus.New,
                LastReviewedAt = null
            }
        };

        Flashcards.AddRange(cards);
        SaveChanges();
    }
}
