using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ScreenAIAssistant.Models;
using ScreenAIAssistant.ViewModels;

namespace ScreenAIAssistant.Helpers;

/// <summary>
/// Attached Behavior hỗ trợ:
/// 1. Kéo thả (Drag &amp; Drop) các thẻ Flashcard / Note ghim khắp trên Bảng Blueprint (EnableDrag="True").
/// 2. Bàn tay kéo di chuyển chiếc bảng Blueprint (Hand Pan) &amp; Giữ Ctrl + Lăn chuột để Phóng to / Thu nhỏ (EnableBoardPanAndZoom="True").
/// Tuân thủ 100% kiến trúc MVVM (không viết code trong View.xaml.cs).
/// </summary>
public static class CanvasDragHelper
{
    #region 1. Kéo thả từng tờ Note Pinned trên Bảng Blueprint (EnableDrag)

    public static readonly DependencyProperty EnableDragProperty =
        DependencyProperty.RegisterAttached(
            "EnableDrag",
            typeof(bool),
            typeof(CanvasDragHelper),
            new PropertyMetadata(false, OnEnableDragChanged));

    public static bool GetEnableDrag(DependencyObject obj) => (bool)obj.GetValue(EnableDragProperty);
    public static void SetEnableDrag(DependencyObject obj, bool value) => obj.SetValue(EnableDragProperty, value);

    private static bool _isDraggingNote;
    private static Point _lastMousePositionOnCanvas;
    private static FrameworkElement? _draggedElement;

    private static void OnEnableDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;

        if ((bool)e.NewValue)
        {
            element.MouseLeftButtonDown += OnNoteMouseLeftButtonDown;
            element.MouseMove += OnNoteMouseMove;
            element.MouseLeftButtonUp += OnNoteMouseLeftButtonUp;
            element.LostMouseCapture += OnNoteLostMouseCapture;
        }
        else
        {
            element.MouseLeftButtonDown -= OnNoteMouseLeftButtonDown;
            element.MouseMove -= OnNoteMouseMove;
            element.MouseLeftButtonUp -= OnNoteMouseLeftButtonUp;
            element.LostMouseCapture -= OnNoteLostMouseCapture;
        }
    }

    private static void OnNoteMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        // Không bắt kéo nếu người dùng đang bấm vào nút bấm (Button) bên trong thẻ Note
        if (IsClickInsideInteractiveControl(e.OriginalSource as DependencyObject, element))
        {
            return;
        }

        var canvas = FindParentCanvas(element);
        if (canvas == null) return;

        _isDraggingNote = true;
        _draggedElement = element;
        _lastMousePositionOnCanvas = e.GetPosition(canvas);

        var container = FindCanvasChildContainer(element, canvas);
        if (container != null)
        {
            BringToFront(canvas, container);
        }

        element.CaptureMouse();
        e.Handled = true;
    }

    private static void OnNoteMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingNote || _draggedElement == null) return;
        if (_draggedElement.DataContext is not PinnedFlashcardItem item) return;

        var canvas = FindParentCanvas(_draggedElement);
        if (canvas == null) return;

        Point currentPos = e.GetPosition(canvas);
        double deltaX = currentPos.X - _lastMousePositionOnCanvas.X;
        double deltaY = currentPos.Y - _lastMousePositionOnCanvas.Y;

        double maxX = Math.Max(400, canvas.ActualWidth - 260);
        double maxY = Math.Max(400, canvas.ActualHeight - 160);

        item.CanvasX = Math.Clamp(item.CanvasX + deltaX, 8, maxX);
        item.CanvasY = Math.Clamp(item.CanvasY + deltaY, 8, maxY);

        _lastMousePositionOnCanvas = currentPos;
        e.Handled = true;
    }

    private static void OnNoteMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingNote) return;

        // Lưu biến cục bộ TRƯỚC KHI gọi ReleaseMouseCapture() vì ReleaseMouseCapture() sẽ kích hoạt OnNoteLostMouseCapture ngay lập tức
        var element = _draggedElement;
        var pinnedItem = element?.DataContext as PinnedFlashcardItem;

        _isDraggingNote = false;
        _draggedElement = null;

        try
        {
            element?.ReleaseMouseCapture();
            pinnedItem?.NotifyPositionSaved();
        }
        catch
        {
            // Bỏ qua lỗi phụ khi nhả chuột
        }

        e.Handled = true;
    }

    private static void OnNoteLostMouseCapture(object sender, MouseEventArgs e)
    {
        _isDraggingNote = false;
        _draggedElement = null;
    }

    #endregion

    #region 2. Bàn tay kéo di chuyển Bảng Blueprint (Hand Pan) & Ctrl + Lăn chuột Phóng to/Thu nhỏ

    public static readonly DependencyProperty EnableBoardPanAndZoomProperty =
        DependencyProperty.RegisterAttached(
            "EnableBoardPanAndZoom",
            typeof(bool),
            typeof(CanvasDragHelper),
            new PropertyMetadata(false, OnEnableBoardPanAndZoomChanged));

    public static bool GetEnableBoardPanAndZoom(DependencyObject obj) => (bool)obj.GetValue(EnableBoardPanAndZoomProperty);
    public static void SetEnableBoardPanAndZoom(DependencyObject obj, bool value) => obj.SetValue(EnableBoardPanAndZoomProperty, value);

    private static bool _isPanningBoard;
    private static Point _panStartMousePoint;
    private static double _panStartHorizontalOffset;
    private static double _panStartVerticalOffset;
    private static ScrollViewer? _panningScrollViewer;

    private static void OnEnableBoardPanAndZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer) return;

        if ((bool)e.NewValue)
        {
            scrollViewer.Cursor = Cursors.Hand;
            scrollViewer.PreviewMouseWheel += OnBoardPreviewMouseWheel;
            scrollViewer.MouseLeftButtonDown += OnBoardMouseLeftButtonDown;
            scrollViewer.MouseMove += OnBoardMouseMove;
            scrollViewer.MouseLeftButtonUp += OnBoardMouseLeftButtonUp;
            scrollViewer.LostMouseCapture += OnBoardLostMouseCapture;
        }
        else
        {
            scrollViewer.PreviewMouseWheel -= OnBoardPreviewMouseWheel;
            scrollViewer.MouseLeftButtonDown -= OnBoardMouseLeftButtonDown;
            scrollViewer.MouseMove -= OnBoardMouseMove;
            scrollViewer.MouseLeftButtonUp -= OnBoardMouseLeftButtonUp;
            scrollViewer.LostMouseCapture -= OnBoardLostMouseCapture;
        }
    }

    private static void OnBoardPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;

        // Khi giữ phím Ctrl và lăn chuột: Phóng to / Thu nhỏ chiếc bảng Blueprint
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (scrollViewer.DataContext is SubjectWorkspaceViewModel vm)
            {
                if (e.Delta > 0)
                {
                    vm.ZoomInCanvas();
                }
                else if (e.Delta < 0)
                {
                    vm.ZoomOutCanvas();
                }
                e.Handled = true;
            }
        }
    }

    private static void OnBoardMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        if (_isDraggingNote) return;

        // Nếu bấm vào thanh cuộn (ScrollBar), nút bấm hoặc đang bấm vào tờ Note Pinned thì không kích hoạt kéo bảng
        if (IsClickOnScrollBarOrNoteOrButton(e.OriginalSource as DependencyObject, scrollViewer))
        {
            return;
        }

        _isPanningBoard = true;
        _panningScrollViewer = scrollViewer;
        _panStartMousePoint = e.GetPosition(scrollViewer);
        _panStartHorizontalOffset = scrollViewer.HorizontalOffset;
        _panStartVerticalOffset = scrollViewer.VerticalOffset;

        scrollViewer.Cursor = Cursors.ScrollAll;
        scrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private static void OnBoardMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanningBoard || _panningScrollViewer == null) return;

        Point currentPoint = e.GetPosition(_panningScrollViewer);
        double deltaX = currentPoint.X - _panStartMousePoint.X;
        double deltaY = currentPoint.Y - _panStartMousePoint.Y;

        _panningScrollViewer.ScrollToHorizontalOffset(Math.Max(0, _panStartHorizontalOffset - deltaX));
        _panningScrollViewer.ScrollToVerticalOffset(Math.Max(0, _panStartVerticalOffset - deltaY));
        e.Handled = true;
    }

    private static void OnBoardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanningBoard) return;

        var sv = _panningScrollViewer;
        _isPanningBoard = false;
        _panningScrollViewer = null;

        if (sv != null)
        {
            sv.Cursor = Cursors.Hand;
            sv.ReleaseMouseCapture();
        }
        e.Handled = true;
    }

    private static void OnBoardLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_panningScrollViewer != null)
        {
            _panningScrollViewer.Cursor = Cursors.Hand;
        }
        _isPanningBoard = false;
        _panningScrollViewer = null;
    }

    #endregion

    #region Helper Methods

    private static bool IsClickInsideInteractiveControl(DependencyObject? source, DependencyObject stopAt)
    {
        var current = source;
        while (current != null && current != stopAt)
        {
            if (current is ButtonBase)
            {
                return true;
            }
            current = GetParentSafe(current);
        }
        return false;
    }

    private static bool IsClickOnScrollBarOrNoteOrButton(DependencyObject? source, DependencyObject stopAt)
    {
        var current = source;
        while (current != null && current != stopAt)
        {
            if (current is ScrollBar || current is ButtonBase)
            {
                return true;
            }
            if (current is FrameworkElement fe && GetEnableDrag(fe))
            {
                return true;
            }
            current = GetParentSafe(current);
        }
        return false;
    }

    private static DependencyObject? GetParentSafe(DependencyObject current)
    {
        if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
        {
            return VisualTreeHelper.GetParent(current);
        }
        return LogicalTreeHelper.GetParent(current);
    }

    private static Canvas? FindParentCanvas(DependencyObject element)
    {
        var current = GetParentSafe(element);
        while (current != null)
        {
            if (current is Canvas canvas)
            {
                return canvas;
            }
            current = GetParentSafe(current);
        }
        return null;
    }

    private static UIElement? FindCanvasChildContainer(DependencyObject element, Canvas canvas)
    {
        DependencyObject? current = element;
        while (current != null)
        {
            var parent = GetParentSafe(current);
            if (parent == canvas && current is UIElement uiElement)
            {
                return uiElement;
            }
            current = parent;
        }
        return null;
    }

    private static void BringToFront(Canvas canvas, UIElement target)
    {
        int maxZ = 10;
        foreach (UIElement child in canvas.Children)
        {
            int z = Panel.GetZIndex(child);
            if (z > maxZ) maxZ = z;
        }
        Panel.SetZIndex(target, maxZ + 1);
    }

    #endregion
}
