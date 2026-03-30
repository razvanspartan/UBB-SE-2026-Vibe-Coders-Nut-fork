using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
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

        // Show the shell first; schema/seed can block on first LocalDB connection.
        var dispatcher = _window.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        dispatcher.TryEnqueue(async () =>
        {
            try
            {
                var storage = _services.GetRequiredService<IDataStorage>();
                if (storage is SqlDataStorage sql)
                {
                    await Task.Run(() =>
                    {
                        sql.EnsureSchemaCreated();
                        sql.SeedPrebuiltWorkouts();
                        sql.SeedAchievementCatalog();
                    }).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Startup database init failed: {ex}");
            }

            navService.NavigateToClientDashboard(requestRefresh: true);
        });
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
        var connectionString = DatabasePaths.GetSqlServerConnectionString();

        // Primary storage (SQL Server LocalDB); achievements and workout templates live here.
        services.AddSingleton<IDataStorage, SqlDataStorage>();

        // Session and analytics (same DB as IDataStorage).
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IWorkoutAnalyticsStore>(
            new SqlWorkoutAnalyticsStore(connectionString));

        services.AddSingleton<IAnalyticsDashboardRefreshBus, AnalyticsDashboardRefreshBus>();
        services.AddSingleton<IWorkoutDataForwarder, WorkoutDataForwarder>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<ProgressionService>();
        services.AddSingleton<ClientService>();

        services.AddTransient<ClientDashboardViewModel>();
        services.AddTransient<RankShowcaseViewModel>();
        services.AddTransient<ActiveWorkoutViewModel>();
    }
}
