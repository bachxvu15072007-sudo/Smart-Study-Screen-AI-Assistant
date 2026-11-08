using Microsoft.EntityFrameworkCore;
using ScreenAIAssistant.Models;

namespace ScreenAIAssistant.Data;

/// <summary>
/// Thực thi truy xuất dữ liệu SQLite sử dụng EF Core và LINQ bất đồng bộ.
/// </summary>
public class StudyRepository : IStudyRepository
{
    private readonly AppDbContext _context;

    public StudyRepository(AppDbContext context)
    {
        _context = context;
    }

    #region 1. Phân hệ Môn học (Subject - Thành viên 3)

    public async Task<List<Subject>> GetAllSubjectsAsync()
    {
        return await _context.Subjects
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Subject?> GetSubjectByIdAsync(int id)
    {
        return await _context.Subjects
            .Include(s => s.Notes)
            .Include(s => s.Flashcards)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subject> AddSubjectAsync(Subject subject)
    {
        subject.CreatedAt = DateTime.Now;
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task UpdateSubjectAsync(Subject subject)
    {
        var existing = await _context.Subjects.FindAsync(subject.Id);
        if (existing != null)
        {
            existing.Name = subject.Name;
            existing.ColorHex = subject.ColorHex;
            existing.Description = subject.Description;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteSubjectAsync(int id)
    {
        var existing = await _context.Subjects.FindAsync(id);
        if (existing != null)
        {
            _context.Subjects.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsSubjectNameExistsAsync(string name, int? excludeId = null)
    {
        var query = _context.Subjects.AsNoTracking().Where(s => s.Name.ToLower() == name.Trim().ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    #endregion

    #region 2. Phân hệ Ghi chú OCR & AI (SnippetNote - Thành viên 1)

    public async Task<List<SnippetNote>> GetNotesAsync(int? subjectId = null, string? searchKeyword = null, bool? onlyFavorites = null)
    {
        var query = _context.SnippetNotes
            .Include(n => n.Subject)
            .AsNoTracking()
            .AsQueryable();

        if (subjectId.HasValue && subjectId.Value > 0)
        {
            query = query.Where(n => n.SubjectId == subjectId.Value);
        }

        if (onlyFavorites.HasValue && onlyFavorites.Value)
        {
            query = query.Where(n => n.IsFavorite);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.Trim().ToLower();
            query = query.Where(n => n.Title.ToLower().Contains(keyword)
                                  || n.OcrText.ToLower().Contains(keyword)
                                  || n.PersonalNote.ToLower().Contains(keyword));
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<SnippetNote?> GetNoteByIdAsync(int id)
    {
        return await _context.SnippetNotes
            .Include(n => n.Subject)
            .Include(n => n.Flashcards)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<SnippetNote> AddNoteAsync(SnippetNote note)
    {
        note.CreatedAt = DateTime.Now;
        _context.SnippetNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task UpdateNoteAsync(SnippetNote note)
    {
        var existing = await _context.SnippetNotes.FindAsync(note.Id);
        if (existing != null)
        {
            existing.SubjectId = note.SubjectId;
            existing.Title = note.Title;
            existing.ImagePath = note.ImagePath;
            existing.OcrText = note.OcrText;
            existing.AiResponse = note.AiResponse;
            existing.PersonalNote = note.PersonalNote;
            existing.IsFavorite = note.IsFavorite;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteNoteAsync(int id)
    {
        var existing = await _context.SnippetNotes.FindAsync(id);
        if (existing != null)
        {
            _context.SnippetNotes.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region 3. Phân hệ Thẻ ghi nhớ Flashcard (Flashcard - Thành viên 2)

    public async Task<List<Flashcard>> GetFlashcardsAsync(int? subjectId = null, MasteryStatus? status = null, string? searchKeyword = null, int? documentId = null)
    {
        var query = _context.Flashcards
            .Include(f => f.Subject)
            .Include(f => f.SnippetNote)
            .Include(f => f.Document)
            .AsNoTracking()
            .AsQueryable();

        if (subjectId.HasValue && subjectId.Value > 0)
        {
            query = query.Where(f => f.SubjectId == subjectId.Value);
        }

        if (documentId.HasValue && documentId.Value > 0)
        {
            query = query.Where(f => f.DocumentId == documentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.MasteryStatus == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.Trim().ToLower();
            query = query.Where(f => f.Question.ToLower().Contains(keyword)
                                  || f.Answer.ToLower().Contains(keyword));
        }

        return await query.OrderByDescending(f => f.Id).ToListAsync();
    }

    public async Task<Flashcard?> GetFlashcardByIdAsync(int id)
    {
        return await _context.Flashcards
            .Include(f => f.Subject)
            .Include(f => f.SnippetNote)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Flashcard> AddFlashcardAsync(Flashcard flashcard)
    {
        _context.Flashcards.Add(flashcard);
        await _context.SaveChangesAsync();
        return flashcard;
    }

    public async Task AddFlashcardsRangeAsync(IEnumerable<Flashcard> flashcards)
    {
        _context.Flashcards.AddRange(flashcards);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateFlashcardAsync(Flashcard flashcard)
    {
        var existing = await _context.Flashcards.FindAsync(flashcard.Id);
        if (existing != null)
        {
            existing.SubjectId = flashcard.SubjectId;
            existing.SnippetNoteId = flashcard.SnippetNoteId;
            existing.DocumentId = flashcard.DocumentId;
            existing.Question = flashcard.Question;
            existing.Answer = flashcard.Answer;
            existing.MasteryStatus = flashcard.MasteryStatus;
            existing.LastReviewedAt = flashcard.LastReviewedAt;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMasteryStatusAsync(int flashcardId, MasteryStatus newStatus)
    {
        var existing = await _context.Flashcards.FindAsync(flashcardId);
        if (existing != null)
        {
            existing.MasteryStatus = newStatus;
            existing.LastReviewedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteFlashcardAsync(int id)
    {
        var existing = await _context.Flashcards.FindAsync(id);
        if (existing != null)
        {
            _context.Flashcards.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region 4. Thống kê Dashboard (Thành viên 3)

    public async Task<int> GetTotalSubjectsCountAsync()
    {
        return await _context.Subjects.CountAsync();
    }

    public async Task<int> GetTotalNotesCountAsync()
    {
        return await _context.SnippetNotes.CountAsync() + await _context.SubjectDocuments.CountAsync();
    }

    public async Task<int> GetTotalFlashcardsCountAsync()
    {
        return await _context.Flashcards.CountAsync();
    }

    public async Task<int> GetMasteredFlashcardsCountAsync()
    {
        return await _context.Flashcards.CountAsync(f => f.MasteryStatus == MasteryStatus.Mastered);
    }

    public async Task<List<SubjectProgress>> GetSubjectProgressListAsync()
    {
        var subjects = await _context.Subjects
            .Include(s => s.Documents)
            .Include(s => s.Notes)
            .Include(s => s.Flashcards)
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return subjects.Select(s => new SubjectProgress
        {
            Subject = s,
            NotesCount = s.Documents.Count + s.Notes.Count,
            FlashcardsCount = s.Flashcards.Count,
            MasteredCount = s.Flashcards.Count(f => f.MasteryStatus == MasteryStatus.Mastered)
        }).ToList();
    }

    #endregion

    #region 5. Quản lý Tài liệu Học tập của Môn học (SubjectDocument - Thành viên 3)

    public async Task<List<SubjectDocument>> GetSubjectDocumentsAsync(int subjectId)
    {
        return await _context.SubjectDocuments
            .AsNoTracking()
            .Where(d => d.SubjectId == subjectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<SubjectDocument> AddSubjectDocumentAsync(SubjectDocument document)
    {
        document.CreatedAt = DateTime.Now;
        _context.SubjectDocuments.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task UpdateSubjectDocumentAsync(SubjectDocument document)
    {
        var existing = await _context.SubjectDocuments.FindAsync(document.Id);
        if (existing != null)
        {
            existing.Title = document.Title;
            existing.PersonalNote = document.PersonalNote;
            existing.ExtractedContent = document.ExtractedContent;
            existing.PageCount = document.PageCount;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteSubjectDocumentAsync(int id)
    {
        var existing = await _context.SubjectDocuments.FindAsync(id);
        if (existing != null)
        {
            _context.SubjectDocuments.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region 6. Quản lý Phiên học & Thống kê Giờ học (StudySession - Thành viên 3)

    public async Task<List<StudySession>> GetStudySessionsAsync(int? subjectId = null)
    {
        var query = _context.StudySessions.AsNoTracking().AsQueryable();
        if (subjectId.HasValue && subjectId.Value > 0)
        {
            query = query.Where(ss => ss.SubjectId == subjectId.Value);
        }

        return await query
            .OrderByDescending(ss => ss.StartTime)
            .ToListAsync();
    }

    public async Task<StudySession> AddStudySessionAsync(StudySession session)
    {
        _context.StudySessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task DeleteStudySessionAsync(int id)
    {
        var existing = await _context.StudySessions.FindAsync(id);
        if (existing != null)
        {
            _context.StudySessions.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<double> GetTotalStudyHoursThisWeekAsync()
    {
        var sevenDaysAgo = DateTime.Now.Date.AddDays(-7);
        var sessions = await _context.StudySessions
            .AsNoTracking()
            .Where(ss => ss.StartTime >= sevenDaysAgo)
            .ToListAsync();

        int totalSeconds = sessions.Sum(ss => Math.Max(0, ss.DurationSeconds));
        return Math.Round(totalSeconds / 3600.0, 2);
    }

    #endregion
}
