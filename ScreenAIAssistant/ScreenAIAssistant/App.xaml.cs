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

    public App()
    {
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
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MainViewModel>();

        // 3. Cửa sổ chính
        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Khởi tạo CSDL SQLite và nạp dữ liệu mẫu nếu chưa có
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
            dbContext.SeedInitialData();
        }

        // Tải trước dữ liệu Dashboard ban đầu
        var dashboardVm = Services.GetRequiredService<DashboardViewModel>();
        await dashboardVm.LoadDashboardDataAsync();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
