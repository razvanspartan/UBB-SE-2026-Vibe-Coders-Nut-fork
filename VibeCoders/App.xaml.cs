using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders;

public partial class App : Application
{
    private static IServiceProvider? _services;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        var navService = (NavigationService)_services.GetRequiredService<INavigationService>();
        _window = new MainWindow(navService);
        _window.Activate();

        navService.NavigateToClientDashboard(requestRefresh: true);
    }

    /// <summary>
    /// Resolves a service from the DI container. Used by pages that cannot
    /// receive constructor injection (WinUI page activation).
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        if (_services is null)
        {
            throw new InvalidOperationException(
                "Service provider is not initialized. Ensure OnLaunched has run.");
        }

        return _services.GetRequiredService<T>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = DatabasePaths.GetAnalyticsDatabasePath();

        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IWorkoutAnalyticsStore>(
            new SqlWorkoutAnalyticsStore(dbPath));
        services.AddSingleton<IAnalyticsDashboardRefreshBus, AnalyticsDashboardRefreshBus>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<ClientDashboardViewModel>();
    }
}
