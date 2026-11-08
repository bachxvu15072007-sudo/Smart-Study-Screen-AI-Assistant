using System.Windows.Controls;

namespace ScreenAIAssistant.Views;

/// <summary>
/// Tab "Trợ Lý AI" của không gian học tập. Tuân thủ MVVM: chỉ chứa InitializeComponent().
/// DataContext kế thừa từ SubjectWorkspaceView (SubjectWorkspaceViewModel).
/// </summary>
public partial class AiAssistantTabView : UserControl
{
    public AiAssistantTabView()
    {
        InitializeComponent();
    }
}
