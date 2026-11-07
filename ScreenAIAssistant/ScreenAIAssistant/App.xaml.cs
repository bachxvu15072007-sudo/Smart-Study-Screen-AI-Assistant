using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScreenAIAssistant.Data;
using ScreenAIAssistant.ViewModels;

namespace ScreenAIAssistant;

/// <summary>
/// Interaction logic for App.xaml - Cấu hình DI Container và khởi tạo CSDL SQLite.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static readonly string _errorLogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "vs_error.log");

    public App()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogException("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Tầng Dữ liệu (Data Layer)
        services.AddDbContext<AppDbContext>();
        services.AddScoped<IStudyRepository, StudyRepository>();

        // 2. Tầng ViewModels
        services.AddSingleton<DashboardViewModel>();        // Tab 1: Tổng quan
        services.AddSingleton<SubjectsViewModel>();         // Tab 2: Danh sách Môn học
        services.AddSingleton<SubjectWorkspaceViewModel>(); // Không gian Học tập của từng Môn
        services.AddSingleton<CalendarViewModel>();         // Tab 3: Lịch, Deadline & To-do
        services.AddSingleton<MainViewModel>();

        // 3. Cửa sổ chính
        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using (var scope = Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
                dbContext.SeedInitialData();
            }

            var dashboardVm = Services.GetRequiredService<DashboardViewModel>();
            await dashboardVm.LoadDashboardDataAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Closed += (_, _) => Shutdown();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogException("OnStartup", ex);
            throw;
        }
    }

    private static void LogException(string source, Exception ex)
    {
        try
        {
            string fullPath = Path.GetFullPath(_errorLogPath);
            File.WriteAllText(fullPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n");
        }
        catch
        {
            // Ignore logging error
        }
    }
}
