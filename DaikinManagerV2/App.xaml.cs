using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using DaikinManagerV2.Services;
using DaikinManagerV2.ViewModels;

namespace DaikinManagerV2;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    public App()
    {
        this.InitializeComponent();
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services (Singleton - own HttpClient, reuse connections)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDaikinApiService, DaikinApiService>();
        
        // ViewModels
        services.AddSingleton<MainViewModel>();      // Owns timer, survives navigation
        services.AddTransient<ControlsViewModel>();  // Fresh per navigation
        services.AddTransient<DiagnosticsViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private Window? _window;
    
    public static MainWindow? MainWindow => (Current as App)?._window as MainWindow;
}
