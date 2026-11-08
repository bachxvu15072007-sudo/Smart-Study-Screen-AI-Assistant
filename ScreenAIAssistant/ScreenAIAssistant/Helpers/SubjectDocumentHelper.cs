using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ScreenAIAssistant.Models;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ScreenAIAssistant.Helpers;

/// <summary>
/// Đại diện cho một Trang tài liệu (Trang PDF, Trang Word hoặc Slide PowerPoint) hiển thị trực tiếp trên WPF Canvas.
/// Kế thừa ObservableObject để khi trang PDF kết xuất ngầm xong sẽ tự cập nhật ảnh tức thì.
/// </summary>
public partial class DocumentPageSection : ObservableObject
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; } = 1;
    public string PageLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FileType { get; set; } = "PDF";
    public string AccentColor { get; set; } = "#2563EB";
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh kết xuất trực tiếp của trang tài liệu (Trang PDF qua Windows.Data.Pdf hoặc ảnh trích xuất từ Slide/Word) để vẽ lên Canvas.
    /// </summary>
    [ObservableProperty]
    private ImageSource? _pageImage;

    public bool HasPageImage => PageImage != null;
    public bool HasTextContent => !string.IsNullOrWhiteSpace(Content);

    partial void OnPageImageChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasPageImage));
    }

    /// <summary>
    /// Kích thước chuẩn để hiển thị trên vùng Canvas.
    /// </summary>
    public double CanvasWidth { get; set; } = 740;
    public double CanvasHeight { get; set; } = 920;
}

/// <summary>
/// Lớp bao bọc thực thể SubjectDocument để hiển thị trên danh sách tài liệu của Môn học.
/// </summary>
public class SubjectDocumentItem
{
    public SubjectDocument DocumentEntity { get; set; } = new();
    public int Id => DocumentEntity.Id;
    public string Title => DocumentEntity.Title;
    public string FilePath => DocumentEntity.FilePath;
    public string FileName => DocumentEntity.FileName;
    public string FileType => DocumentEntity.FileType;

    public string BadgeText => FileType switch
    {
        "PDF" => "📕 PDF",
        "WORD" => "📘 WORD",
        "PPT" => "📙 PPT",
        _ => "📄 FILE"
    };

    public string BadgeColor => FileType switch
    {
        "PDF" => "#DC2626",   // Đỏ PDF
        "WORD" => "#2563EB",  // Xanh dương Word
        "PPT" => "#D97706",   // Cam PowerPoint
        _ => "#475569"
    };

    public string InfoText => $"{DocumentEntity.PageCount} trang/slide • {Math.Max(1, DocumentEntity.FileSizeKb)} KB";
    public string CreatedAtText => DocumentEntity.CreatedAt.ToString("dd/MM/yyyy HH:mm");
}

/// <summary>
/// Dịch vụ đọc, kết xuất trang tài liệu (.pdf, .docx, .pptx) siêu tốc (Lazy + Cache) lên WPF Canvas.
/// </summary>
public static class SubjectDocumentHelper
{
    private static readonly string _documentsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenAIAssistant",
        "Documents");

    /// <summary>
    /// Bộ nhớ đệm (RAM Cache) lưu các trang đã kết xuất theo đường dẫn file để chuyển tài liệu tức thì (0ms).
    /// </summary>
    private static readonly ConcurrentDictionary<string, List<DocumentPageSection>> _pageCache =
        new(StringComparer.OrdinalIgnoreCase);

    public static void ClearCache()
    {
        _pageCache.Clear();
    }

    public static string GetDocumentsFolder()
    {
        if (!Directory.Exists(_documentsFolder))
        {
            Directory.CreateDirectory(_documentsFolder);
        }
        return _documentsFolder;
    }

    public static string DetectFileType(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "FILE";

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "PDF",
            ".docx" or ".doc" => "WORD",
            ".pptx" or ".ppt" => "PPT",
            _ => "FILE"
        };
    }

    /// <summary>
    /// Sao chép file tài liệu vào thư mục môn học và nạp siêu tốc (chỉ kết xuất trang 1 ngay lập tức, các trang sau nạp ngầm).
    /// </summary>
    public static async Task<(SubjectDocument NewDocument, List<DocumentPageSection> Pages)> ImportDocumentForSubjectAsync(
        string sourceFilePath,
        int subjectId)
    {
        string subjectDir = Path.Combine(GetDocumentsFolder(), $"Subject_{subjectId}");
        if (!Directory.Exists(subjectDir))
        {
            Directory.CreateDirectory(subjectDir);
        }

        string originalFileName = Path.GetFileName(sourceFilePath);
        string safeFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{originalFileName}";
        string destPath = Path.Combine(subjectDir, safeFileName);

        await Task.Run(() => File.Copy(sourceFilePath, destPath, overwrite: true));

        var fileInfo = new FileInfo(destPath);
        long sizeKb = Math.Max(1, fileInfo.Length / 1024);
        string fileType = DetectFileType(destPath);

        var pages = await ReadDocumentPagesAsync(destPath);
        string cleanSummary = BuildCleanSummaryText(pages, originalFileName);

        var docEntity = new SubjectDocument
        {
            SubjectId = subjectId,
            Title = Path.GetFileNameWithoutExtension(originalFileName),
            FileName = originalFileName,
            FilePath = destPath,
            FileType = fileType,
            FileSizeKb = sizeKb,
            PageCount = Math.Max(1, pages.Count),
            ExtractedContent = cleanSummary,
            PersonalNote = string.Empty,
            CreatedAt = DateTime.Now
        };

        return (docEntity, pages);
    }

    /// <summary>
    /// Đọc danh sách trang của tài liệu. Nếu đã có trong Cache thì trả về ngay lập tức (0ms).
    /// </summary>
    public static async Task<List<DocumentPageSection>> ReadDocumentPagesAsync(string? filePath, string? fallbackText = null)
    {
        string fileType = DetectFileType(filePath);

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return BuildFallbackPages(fileType, fallbackText, "Không tìm thấy file gốc trên ổ cứng");
        }

        string fullPath = Path.GetFullPath(filePath);
        if (_pageCache.TryGetValue(fullPath, out var cachedPages) && cachedPages.Count > 0)
        {
            return cachedPages;
        }

        try
        {
            var pages = fileType switch
            {
                "PDF" => await RenderPdfPagesFastAsync(fullPath, fallbackText),
                "WORD" => await Task.Run(() => ReadWordDocxPages(fullPath, fallbackText)),
                "PPT" => await Task.Run(() => ReadPowerPointPptxSlides(fullPath, fallbackText)),
                _ => BuildFallbackPages(fileType, fallbackText, Path.GetFileName(fullPath))
            };

            if (pages.Count > 0)
            {
                _pageCache[fullPath] = pages;
            }
            return pages;
        }
        catch (Exception ex)
        {
            return BuildFallbackPages(fileType, fallbackText, $"Tài liệu {Path.GetFileName(fullPath)} ({ex.Message})");
        }
    }

    /// <summary>
    /// Kết xuất siêu tốc: Chỉ render Trang 1 ngay lập tức (~40ms) để mở lên Canvas tức thì,
    /// còn các trang tiếp theo được kết xuất ngầm trong Background Task.
    /// </summary>
    private static async Task<List<DocumentPageSection>> RenderPdfPagesFastAsync(string fullPath, string? fallbackText)
    {
        var pages = new List<DocumentPageSection>();

        try
        {
            StorageFile storageFile = await StorageFile.GetFileFromPathAsync(fullPath);
            PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);
            int totalPages = (int)pdfDoc.PageCount;
            int maxPages = Math.Min(totalPages, 40);
            string fileName = Path.GetFileName(fullPath);

            for (uint i = 0; i < maxPages; i++)
            {
                using PdfPage page = pdfDoc.GetPage(i);
                double aspect = page.Size.Width > 0 ? page.Size.Height / page.Size.Width : 1.414;
                double canvasW = 920;
                double canvasH = Math.Round(canvasW * aspect);

                BitmapImage? firstPageBitmap = null;
                if (i == 0)
                {
                    firstPageBitmap = await RenderSinglePdfPageBitmapAsync(page);
                }

                pages.Add(new DocumentPageSection
                {
                    PageNumber = (int)i + 1,
                    TotalPages = maxPages,
                    PageLabel = $"📕 TRANG PDF {i + 1} / {totalPages}",
                    Title = $"{fileName} — Trang {i + 1}",
                    Content = string.Empty,
                    FileType = "PDF",
                    AccentColor = "#DC2626",
                    SourceFilePath = fullPath,
                    PageImage = firstPageBitmap,
                    CanvasWidth = canvasW,
                    CanvasHeight = canvasH
                });
            }

            // Nếu có từ 2 trang trở lên, chạy ngầm kết xuất các trang còn lại mà không chặn giao diện
            if (maxPages > 1)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        for (uint i = 1; i < maxPages; i++)
                        {
                            var targetSection = pages[(int)i];
                            if (targetSection.PageImage != null) continue;

                            using PdfPage bgPage = pdfDoc.GetPage(i);
                            var bmp = await RenderSinglePdfPageBitmapAsync(bgPage);
                            if (bmp != null)
                            {
                                targetSection.PageImage = bmp;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Không chặn giao diện; trang chưa kết xuất sẽ được thử lại khi người dùng chuyển tới (EnsurePageRenderedAsync)
                        Debug.WriteLine($"[SubjectDocumentHelper] Lỗi kết xuất ngầm PDF: {ex}");
                    }
                });
            }

            if (pages.Count > 0)
            {
                return pages;
            }
        }
        catch (Exception ex)
        {
            // Dự phòng nếu PDF không mở được bằng WinRT: hiển thị lý do lên trang thay vì im lặng
            Debug.WriteLine($"[SubjectDocumentHelper] Không mở được PDF: {ex}");
            return BuildFallbackPages("PDF", fallbackText, $"{Path.GetFileName(fullPath)} (không đọc được PDF: {ex.Message})");
        }

        return BuildFallbackPages("PDF", fallbackText, Path.GetFileName(fullPath));
    }

    /// <summary>
    /// Đảm bảo một trang PDF bất kỳ được kết xuất ngay khi người dùng bấm chuyển tới trang đó.
    /// </summary>
    public static async Task EnsurePageRenderedAsync(DocumentPageSection? section)
    {
        if (section == null || section.PageImage != null || section.FileType != "PDF")
            return;

        if (string.IsNullOrWhiteSpace(section.SourceFilePath) || !File.Exists(section.SourceFilePath))
            return;

        try
        {
            StorageFile storageFile = await StorageFile.GetFileFromPathAsync(section.SourceFilePath);
            PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);
            uint pageIndex = (uint)Math.Max(0, section.PageNumber - 1);
            if (pageIndex < pdfDoc.PageCount)
            {
                using PdfPage page = pdfDoc.GetPage(pageIndex);
                var bmp = await RenderSinglePdfPageBitmapAsync(page);
                if (bmp != null)
                {
                    section.PageImage = bmp;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SubjectDocumentHelper] Lỗi kết xuất trang {section.PageNumber}: {ex}");
            if (string.IsNullOrWhiteSpace(section.Content))
            {
                section.Content = $"Không thể kết xuất trang này: {ex.Message}";
            }
        }
    }

    private static async Task<BitmapImage?> RenderSinglePdfPageBitmapAsync(PdfPage page)
    {
        using var stream = new InMemoryRandomAccessStream();
        // Nâng độ nét Ultra-HD: Tính theo tỷ lệ 4.0x kích thước chuẩn (2400px - 2800px) để hiển thị sắc nét từng nét chữ
        uint targetWidth = (uint)Math.Clamp(page.Size.Width * 4.0, 2400, 2800);
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = targetWidth
        };

        await page.RenderToStreamAsync(stream, options);

        var bitmap = new BitmapImage();
        using (var netStream = stream.AsStreamForRead())
        {
            netStream.Position = 0;
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = netStream;
            bitmap.EndInit();
            bitmap.Freeze();
        }
        return bitmap;
    }

    private static List<DocumentPageSection> ReadWordDocxPages(string filePath, string? fallbackText)
    {
        var pages = new List<DocumentPageSection>();
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".doc")
        {
            return BuildFallbackPages(
                "WORD",
                $"Tài liệu Microsoft Word định dạng cũ (.doc): {Path.GetFileName(filePath)}\n\n" +
                "• Định dạng .doc nhị phân cũ (Word 97-2003) đã được lưu vào CSDL của môn học.\n" +
                "• Bạn có thể bấm nút '↗ Mở file gốc' ở góc trên phải để xem trực tiếp trong Microsoft Word, hoặc lưu file dưới dạng .docx để hiển thị từng trang trực tiếp trên Canvas.",
                Path.GetFileName(filePath));
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

        var docEntry = archive.GetEntry("word/document.xml");
        if (docEntry == null)
        {
            return BuildFallbackPages("WORD", fallbackText, Path.GetFileName(filePath));
        }

        var mediaImages = ExtractMediaBitmapsFromZip(archive, "word/media/");

        using var entryStream = docEntry.Open();
        var xdoc = XDocument.Load(entryStream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphs = xdoc.Descendants(w + "p")
            .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (paragraphs.Count == 0 && mediaImages.Count == 0)
        {
            return BuildFallbackPages("WORD", fallbackText, Path.GetFileName(filePath));
        }

        const int paragraphsPerPage = 8;
        int totalPages = Math.Max(1, (int)Math.Ceiling(paragraphs.Count / (double)paragraphsPerPage));
        int pageNum = 1;

        for (int i = 0; i < Math.Max(1, paragraphs.Count); i += paragraphsPerPage)
        {
            var chunk = paragraphs.Skip(i).Take(paragraphsPerPage).ToList();
            string title = chunk.FirstOrDefault() ?? $"{Path.GetFileNameWithoutExtension(filePath)} - Trang {pageNum}";
            string body = chunk.Count > 1
                ? string.Join("\n\n", chunk.Skip(1))
                : (chunk.FirstOrDefault() ?? string.Empty);

            ImageSource? embeddedImg = (pageNum - 1) < mediaImages.Count ? mediaImages[pageNum - 1] : null;

            pages.Add(new DocumentPageSection
            {
                PageNumber = pageNum,
                TotalPages = totalPages,
                PageLabel = $"📘 TRANG WORD {pageNum} / {totalPages}",
                Title = title.Length > 100 ? title[..100] + "..." : title,
                Content = body,
                FileType = "WORD",
                AccentColor = "#2563EB",
                SourceFilePath = filePath,
                PageImage = embeddedImg,
                CanvasWidth = 920,
                CanvasHeight = embeddedImg != null ? 900 : 680
            });
            pageNum++;
        }

        return pages;
    }

    private static List<DocumentPageSection> ReadPowerPointPptxSlides(string filePath, string? fallbackText)
    {
        var slides = new List<DocumentPageSection>();
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".ppt")
        {
            return BuildFallbackPages(
                "PPT",
                $"Bài giảng PowerPoint định dạng cũ (.ppt): {Path.GetFileName(filePath)}\n\n" +
                "• File .ppt đã được lưu vào CSDL của môn học.\n" +
                "• Bạn có thể bấm nút '↗ Mở file gốc' để trình chiếu trong PowerPoint, hoặc lưu dưới dạng .pptx để hiển thị từng Slide trên Canvas.",
                Path.GetFileName(filePath));
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

        var slideEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase)
                     && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => ExtractSlideNumber(e.Name))
            .ToList();

        if (slideEntries.Count == 0)
        {
            return BuildFallbackPages("PPT", fallbackText, Path.GetFileName(filePath));
        }

        var mediaImages = ExtractMediaBitmapsFromZip(archive, "ppt/media/");
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        int slideIdx = 1;
        int totalSlides = slideEntries.Count;

        foreach (var entry in slideEntries)
        {
            using var stream = entry.Open();
            var xdoc = XDocument.Load(stream);

            var lines = xdoc.Descendants(a + "p")
                .Select(p => string.Concat(p.Descendants(a + "t").Select(t => t.Value)).Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            string slideTitle = lines.FirstOrDefault() ?? $"Slide {slideIdx}";
            var bulletLines = lines.Skip(1).Select(l => $"• {l}").ToList();
            string slideBody = bulletLines.Count > 0
                ? string.Join("\n\n", bulletLines)
                : (lines.Count > 0 ? $"• {slideTitle}" : "(Slide hình ảnh minh họa)");

            ImageSource? slideImg = (slideIdx - 1) < mediaImages.Count ? mediaImages[slideIdx - 1] : null;

            slides.Add(new DocumentPageSection
            {
                PageNumber = slideIdx,
                TotalPages = totalSlides,
                PageLabel = $"📙 SLIDE {slideIdx} / {totalSlides}",
                Title = slideTitle,
                Content = slideBody,
                FileType = "PPT",
                AccentColor = "#D97706",
                SourceFilePath = filePath,
                PageImage = slideImg,
                CanvasWidth = 920,
                CanvasHeight = slideImg != null ? 680 : 480
            });
            slideIdx++;
        }

        return slides;
    }

    private static List<ImageSource> ExtractMediaBitmapsFromZip(ZipArchive archive, string prefix)
    {
        var list = new List<ImageSource>();
        var imgEntries = archive.Entries
            .Where(e => e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                     && (e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                      || e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                      || e.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)))
            .Take(12)
            .ToList();

        foreach (var entry in imgEntries)
        {
            try
            {
                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                ms.Position = 0;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bitmapDecode(bmp, ms);
                bmp.EndInit();
                bmp.Freeze();
                list.Add(bmp);
            }
            catch
            {
                // Bỏ qua ảnh lỗi định dạng
            }
        }

        return list;
    }

    private static void bitmapDecode(BitmapImage bmp, MemoryStream ms)
    {
        // Cho phép giải mã ảnh trích xuất từ slide PowerPoint / Word lên đến độ phân giải cao 2048px
        bmp.DecodePixelWidth = 2048;
        bmp.StreamSource = ms;
    }

    private static List<DocumentPageSection> BuildFallbackPages(string fileType, string? fallbackText, string defaultTitle)
    {
        string color = fileType switch
        {
            "PDF" => "#DC2626",
            "WORD" => "#2563EB",
            "PPT" => "#D97706",
            _ => "#475569"
        };

        string labelPrefix = fileType switch
        {
            "PDF" => "📕 TRANG PDF",
            "WORD" => "📘 TRANG WORD",
            "PPT" => "📙 SLIDE",
            _ => "📄 TRANG"
        };

        string text = string.IsNullOrWhiteSpace(fallbackText)
            ? $"Đã nạp tài liệu: {defaultTitle}.\nBạn có thể ghim Note / Flashcard trên Bảng Blueprint hoặc bấm '↗ Mở file gốc' để xem trên ứng dụng ngoài."
            : fallbackText;

        return new List<DocumentPageSection>
        {
            new()
            {
                PageNumber = 1,
                TotalPages = 1,
                PageLabel = $"{labelPrefix} 1 / 1",
                Title = defaultTitle,
                Content = text,
                FileType = fileType,
                AccentColor = color,
                CanvasWidth = 920,
                CanvasHeight = 560
            }
        };
    }

    private static string BuildCleanSummaryText(List<DocumentPageSection> pages, string fileName)
    {
        var sb = new StringBuilder();
        foreach (var p in pages.Take(6))
        {
            sb.AppendLine($"[{p.PageLabel}] {p.Title}");
            if (!string.IsNullOrWhiteSpace(p.Content))
            {
                sb.AppendLine(p.Content);
            }
        }

        string result = sb.ToString().Replace("\0", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            result = $"Tài liệu {fileName} ({pages.Count} trang/slide).";
        }

        return result.Length > 3000 ? result[..3000] : result;
    }

    private static int ExtractSlideNumber(string fileName)
    {
        string digits = new string(fileName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int n) ? n : 999;
    }

    public static void OpenWithDefaultProgram(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("Không tìm thấy file tài liệu trên ổ đĩa.", filePath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}
