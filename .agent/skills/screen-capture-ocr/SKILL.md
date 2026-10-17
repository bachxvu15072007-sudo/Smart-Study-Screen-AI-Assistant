---
name: screen-capture-ocr
description: >-
  Guides the implementation of the transparent OverlayWindow with 2D CombinedGeometry,
  DPI-aware screen region capture, Clipboard/File image import, and Windows.Media.Ocr text extraction.
  Use this skill when working on Member 1 tasks (OverlayWindow, ScreenCaptureService, WindowsOcrService, DpiHelper).
---

# Skill: Chụp Vùng Màn Hình Chuẩn DPI & Nhận Diện Chữ Windows OCR (`screen-capture-ocr`)

Sử dụng skill này khi cài đặt hoặc gỡ lỗi tính năng chụp màn hình đồ họa 2D và nhận diện chữ OCR của **Thành viên 1**.

## 1. Cấu hình `.csproj` Bắt buộc cho WinRT `Windows.Media.Ocr`
Trong `ScreenAIAssistant.csproj`, `TargetFramework` bắt buộc phải khai báo phiên bản Windows SDK:
```xml
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
```
*(Bật `<UseWindowsForms>true</UseWindowsForms>` hoặc cài `System.Drawing.Common` để sử dụng `Bitmap` và `Graphics.CopyFromScreen`).*

## 2. Quy đổi Tọa độ Chuột sang Điểm ảnh Vật lý (DPI Scaling Fix)
Khi màn hình laptop để Scale 125% hoặc 150%, tọa độ trên `Canvas` của WPF là đơn vị logic (DIPs). Bắt buộc nhân với hệ số DPI trước khi cắt ảnh:

```csharp
var dpi = VisualTreeHelper.GetDpi(overlayWindow);
int physicalX = (int)Math.Round(selectionRect.X * dpi.DpiScaleX);
int physicalY = (int)Math.Round(selectionRect.Y * dpi.DpiScaleY);
int physicalWidth = (int)Math.Round(selectionRect.Width * dpi.DpiScaleX);
int physicalHeight = (int)Math.Round(selectionRect.Height * dpi.DpiScaleY);
```

## 3. Hiệu ứng Khoét Lỗ Trong Suốt trên `OverlayWindow` (Tiêu chí 3' - Đồ họa 2D)
Sử dụng `Path` với `CombinedGeometry` (`GeometryCombineMode="Exclude"`) giữa hình chữ nhật toàn màn hình và hình chữ nhật vùng chọn (`SelectionGeometry`) để làm tối vùng xung quanh nhưng giữ sáng rõ vùng văn bản đang quét.

## 4. Mẫu Hàm Nhận diện Chữ `WindowsOcrService` (~25 dòng)
```csharp
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

public async Task<string> ExtractTextFromFileAsync(string imagePath)
{
    var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(imagePath));
    using var stream = await file.OpenAsync(FileAccessMode.Read);
    var decoder = await BitmapDecoder.CreateAsync(stream);
    using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

    var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages()
                    ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
    if (ocrEngine == null)
        return string.Empty;

    var result = await ocrEngine.RecognizeAsync(softwareBitmap);
    return result?.Text ?? string.Empty;
}
```
