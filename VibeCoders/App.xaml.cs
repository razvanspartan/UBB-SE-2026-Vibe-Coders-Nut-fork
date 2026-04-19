using System;
using System.Diagnostics;
using System.Linq;
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
    public Window? _window;

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
        var achievementBus = _services.GetRequiredService<IAchievementUnlockedBus>();

        try
        {
            var storage = _services.GetRequiredService<IDataStorage>();
            if (storage is SqlDataStorage sql)
            {
                sql.EnsureSchemaCreated();
                sql.SeedPrebuiltWorkouts();
                sql.SeedAchievementCatalog();
                sql.SeedWorkoutMilestoneAchievements();
                sql.SeedEvaluationEngineAchievements();
                sql.SeedTestData();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup database init failed: {ex}");
        }

        TrySyncDemoClientSession();
        _window = new MainWindow(navService, achievementBus);
        _window.Activate();

        navService.NavigateToClientDashboard(requestRefresh: true);
    }

    public static T GetService<T>() where T : notnull
    {
        if (_services is null)
        {
            throw new InvalidOperationException(
                "Service provider is not initialized. Ensure OnLaunched has run.");
        }

        return _services.GetRequiredService<T>();
    }

    private void TrySyncDemoClientSession()
    {
        if (_services is null) return;

        try
        {
            var storage = _services.GetRequiredService<IDataStorage>();
            var session = _services.GetRequiredService<IUserSession>();
            var user    = storage.LoadUser("TestClient");
            if (user is null) return;

            var roster = storage.GetTrainerClient(1);
            var client = roster.FirstOrDefault(c =>
                string.Equals(c.Username, "TestClient", StringComparison.OrdinalIgnoreCase));
            if (client is null) return;

            session.CurrentUserId   = user.Id;
            session.CurrentClientId = client.Id;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Session sync skipped: {ex.Message}");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var connectionString = DatabasePaths.GetConnectionString();

        services.AddSingleton<IDataStorage, SqlDataStorage>();

        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IWorkoutAnalyticsStore>(
            new SqlWorkoutAnalyticsStore(connectionString));

        services.AddSingleton<IAnalyticsDashboardRefreshBus, AnalyticsDashboardRefreshBus>();
        services.AddSingleton<IAchievementUnlockedBus, AchievementUnlockedBus>();
        services.AddSingleton<IWorkoutDataForwarder, WorkoutDataForwarder>();
        services.AddSingleton<ICalendarExportService, CalendarExportService>();
        services.AddSingleton<ICalendarWorkoutCatalogService, CalendarWorkoutCatalogService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<WorkoutUiState>();

        services.AddHttpClient();

        services.AddSingleton<ProgressionService>();
        services.AddSingleton<ClientService>();
        services.AddSingleton<EvaluationEngine>();
        services.AddSingleton<TrainerService>();

        services.AddTransient<ClientDashboardViewModel>();
        services.AddTransient<CalendarIntegrationViewModel>();
        services.AddTransient<RankShowcaseViewModel>();
        services.AddTransient<ActiveWorkoutViewModel>();
        services.AddTransient<WorkoutLogsViewModel>();
        services.AddTransient<CreateWorkoutViewModel>();
        services.AddTransient<AchievementsViewModel>();
        services.AddTransient<ClientProfileViewModel>();

        services.AddTransient<TrainerDashboardViewModel>(sp =>
        {
            var trainerService = sp.GetRequiredService<TrainerService>();
            var navService = sp.GetRequiredService<INavigationService>();
            return new TrainerDashboardViewModel(trainerService, navService);
        });
    }
}
