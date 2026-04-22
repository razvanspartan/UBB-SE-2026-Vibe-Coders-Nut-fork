namespace VibeCoders;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using VibeCoders.Repositories;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;

public partial class App : Application
{
    private static IServiceProvider? servicesProvider;

    public App()
    {
        this.InitializeComponent();
    }

    public Window? Window { get; set; }

    public static T GetService<T>()
        where T : notnull
    {
        if (servicesProvider is null)
        {
            throw new InvalidOperationException(
                "Service provider is not initialized. Ensure OnLaunched has run.");
        }

        return servicesProvider.GetRequiredService<T>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        servicesProvider = services.BuildServiceProvider();

        var navService = (NavigationService)servicesProvider.GetRequiredService<INavigationService>();
        var achievementBus = servicesProvider.GetRequiredService<IAchievementUnlockedBus>();

        try
        {
            var storage = servicesProvider.GetRequiredService<DatabaseSchemaManager>();
            storage.EnsureSchemaCreated();
            var initializer = servicesProvider.GetRequiredService<DatabaseDataInitializer>();
            initializer.SeedPrebuiltWorkouts();
            initializer.SeedAchievementCatalog();
                initializer.SeedWorkoutMilestoneAchievements();
                initializer.SeedEvaluationEngineAchievements();
                initializer.SeedTestData();
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Startup database init failed: {exception}");
        }

        this.TrySyncDemoClientSession();
        this.Window = new MainWindow(navService, achievementBus);
        this.Window.Activate();

        navService.NavigateToClientDashboard(requestRefresh: true);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var connectionString = DatabasePaths.GetConnectionString();
        services.AddSingleton(new DatabaseDataInitializer(connectionString));
        services.AddSingleton(new DatabaseSchemaManager(connectionString));
        services.AddSingleton<IClientService, ClientService>();
        services.AddSingleton<IRepositoryWorkoutTemplate>(new RepositoryWorkoutTemplate(connectionString));
        services.AddSingleton<IRepositoryNutrition>(new RepositoryNutrition(connectionString));
        services.AddSingleton<IRepositoryAchievements>(new RepositoryAchievements(connectionString));
        services.AddSingleton<IRepositoryNotification>(new RepositoryNotification(connectionString));
        services.AddSingleton<IRepositoryWorkoutLog>(new RepositoryWorkoutLog(connectionString));
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IWorkoutAnalyticsStore>(
            new SqlWorkoutAnalyticsStore(connectionString));
        services.AddSingleton<IRepositoryTrainer>(new RepositoryTrainer(connectionString));
        services.AddSingleton<IAnalyticsDashboardRefreshBus, AnalyticsDashboardRefreshBus>();
        services.AddSingleton<IAchievementUnlockedBus, AchievementUnlockedBus>();
        services.AddSingleton<IWorkoutDataForwarder, WorkoutDataForwarder>();
        services.AddSingleton<ICalendarExportService, CalendarExportService>();
        services.AddSingleton<ICalendarWorkoutCatalogService, CalendarWorkoutCatalogService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<WorkoutUiState>();

        services.AddHttpClient();

        services.AddSingleton(new VibeCoders.Models.Integration.NutritionSyncOptions { Endpoint = "http://localhost:5000/api/nutrition/sync" });
        services.AddSingleton<ProgressionService>();
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
        services.AddTransient<TrainerDashboardViewModel>(CreateTrainerDashboardViewModel);
    }

    private static TrainerDashboardViewModel CreateTrainerDashboardViewModel(IServiceProvider serviceProvider)
    {
        var trainerService = serviceProvider.GetRequiredService<TrainerService>();
        var navService = serviceProvider.GetRequiredService<INavigationService>();
        return new TrainerDashboardViewModel(trainerService, navService);
    }

    private void TrySyncDemoClientSession()
    {
        if (servicesProvider is null)
        {
            return;
        }

        try
        {
            var trainerRepository = servicesProvider.GetRequiredService<IRepositoryTrainer>();
            var session = servicesProvider.GetRequiredService<IUserSession>();
            var user = trainerRepository.LoadUser("TestClient");
            if (user is null)
            {
                return;
            }
            var roster = trainerRepository.GetTrainerClients(1);
            var client = roster.FirstOrDefault(c =>
                string.Equals(c.Username, "TestClient", StringComparison.OrdinalIgnoreCase));
            if (client is null)
            {
                return;
            }

            session.CurrentUserId = user.Id;
            session.CurrentClientId = client.Id;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Session sync skipped: {exception.Message}");
        }
    }
}
