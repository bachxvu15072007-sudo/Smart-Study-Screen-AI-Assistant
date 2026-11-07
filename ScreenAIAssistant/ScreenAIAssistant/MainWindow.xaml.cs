using System.Windows;
using ScreenAIAssistant.ViewModels;

namespace ScreenAIAssistant;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Tuân thủ nghiêm ngặt MVVM: Gán DataContext từ DI và chỉ gọi InitializeComponent.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
