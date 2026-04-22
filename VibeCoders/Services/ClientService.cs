namespace VibeCoders.Services;

using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services.Interfaces;

public class ClientService : IClientService
{
    private readonly IProgressionService progressionService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IEvaluationEngine evaluationEngine;
    private readonly IAchievementUnlockedBus achievementBus;
    private readonly NutritionSyncOptions nutritionSync;
    private readonly IRepositoryWorkoutLog workoutLogRepository;
    private readonly IRepositoryTrainer trainerRepository;
    private readonly IRepositoryNotification notificationRepository;
    private readonly IRepositoryAchievements achievementsRepository;
    private readonly IRepositoryNutrition nutritionRepository;
    private readonly IRepositoryWorkoutTemplate workoutTemplateRepository;

    public ClientService(
        IRepositoryWorkoutLog workoutLogRepository,
        IProgressionService progressionService,
        IHttpClientFactory httpClientFactory,
        IEvaluationEngine evaluationEngine,
        IAchievementUnlockedBus achievementBus,
        NutritionSyncOptions nutritionSync,
        IRepositoryTrainer trainerRepository,
        IRepositoryNotification notificationRepository,
        IRepositoryAchievements achievementsRepository,
        IRepositoryNutrition nutritionRepository,
        IRepositoryWorkoutTemplate workoutTemplateRepository)
    {
        this.workoutLogRepository = workoutLogRepository;
        this.progressionService = progressionService;
        this.httpClientFactory = httpClientFactory;
        this.notificationRepository = notificationRepository;
        this.evaluationEngine = evaluationEngine;
        this.achievementBus = achievementBus;
        this.nutritionSync = nutritionSync;
        this.trainerRepository = trainerRepository;
        this.nutritionRepository = nutritionRepository;
        this.achievementsRepository = achievementsRepository;
        this.workoutTemplateRepository = workoutTemplateRepository;
    }

    public List<WorkoutLog> GetWorkoutHistoryForClient(int clientId)
    {
        try
        {
            return this.workoutLogRepository.GetWorkoutHistory(clientId);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading workout history for client {clientId}: {exception.Message}");
            return new List<WorkoutLog>();
        }
    }

    public bool UpdateWorkoutLog(WorkoutLog updatedWorkoutLog)
    {
        try
        {
            return this.workoutLogRepository.UpdateWorkoutLog(updatedWorkoutLog);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating workout log {updatedWorkoutLog.Id}: {exception.Message}");
            return false;
        }
    }

    public bool FinalizeWorkout(WorkoutLog log)
    {
        if (log == null || log.Exercises == null)
        {
            return false;
        }

        try
        {
            log.Date = DateTime.Now;
            this.progressionService.EvaluateWorkout(log);

            ComputeCalories(log);

            bool isSaved = this.workoutLogRepository.SaveWorkoutLog(log);
            if (!isSaved)
            {
                return false;
            }
            RunAchievementEvaluation(log.ClientId);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finalizing workout: {ex.Message}");
            return false;
        }
    }

    public bool SaveSet(WorkoutLog log, string exerciseName, LoggedSet set)
    {
        if (log == null || set == null || string.IsNullOrWhiteSpace(exerciseName))
        {
            return false;
        }

        try
        {
            var exercise = log.Exercises
                .FirstOrDefault(e => e.ExerciseName == exerciseName);

            if (exercise == null)
            {
                exercise = new LoggedExercise
                {
                    ExerciseName = exerciseName,
                    WorkoutLogId = log.Id
                };
                log.Exercises.Add(exercise);
            }

            set.SetIndex = exercise.Sets.Count + 1;
            set.WorkoutLogId = log.Id;
            set.ExerciseName = exerciseName;
            exercise.Sets.Add(set);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving set: {ex.Message}");
            return false;
        }
    }

    public bool ModifyWorkout(WorkoutLog updatedLog)
    {
        if (updatedLog == null)
        {
            return false;
        }

        try
        {
            return this.workoutLogRepository.SaveWorkoutLog(updatedLog);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error modifying workout: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SyncNutritionAsync(
        NutritionSyncPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = this.httpClientFactory.CreateClient();

            var json = System.Text.Json.JsonSerializer.Serialize(
                payload,
                NutritionSyncJsonContext.Default.NutritionSyncPayload);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client
                .PostAsync(this.nutritionSync.Endpoint, content, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing nutrition: {ex.Message}");
            return false;
        }
    }

    public NutritionSyncPayload BuildNutritionSyncPayload(int clientId)
    {
        var history = this.workoutLogRepository.GetWorkoutHistory(clientId);
        var totalCalories = history.Sum(h => h.TotalCaloriesBurned);
        var last = history.FirstOrDefault();
        var difficulty = string.IsNullOrWhiteSpace(last?.IntensityTag) ? "unknown" : last.IntensityTag;

        float bmi = 0f;
        List<Client> roster;
        try
        {
            roster = this.trainerRepository.GetTrainerClients(1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClientService: BMI lookup failed; sync continues with UserBmi=0. {ex.Message}");
            roster =
                [];
        }

        var profileClient = roster.FirstOrDefault(c => c.Id == clientId);
        if (profileClient is { Weight: > 0, Height: > 0 })
        {
            bmi = (float)BmiCalculator.CalculateBmi(profileClient.Weight, profileClient.Height);
        }

        return new NutritionSyncPayload
        {
            TotalCalories = totalCalories,
            WorkoutDifficulty = difficulty,
            UserBmi = bmi,
        };
    }

    public ClientProfileSnapshot BuildClientProfileSnapshot(int clientId)
    {
        var history = this.workoutLogRepository.GetWorkoutHistory(clientId);
        var totalCal = history.Sum(h => h.TotalCaloriesBurned);

        var latest = history.FirstOrDefault();
        string latestSessionHint;
        IReadOnlyList<LoggedExercise> loggedExercises;

        if (latest != null && latest.Exercises is { Count: > 0 })
        {
            latestSessionHint = $"Latest session: {latest.WorkoutName} — {latest.Date:g}";
            loggedExercises = latest.Exercises;
        }
        else
        {
            latestSessionHint = "No completed workouts with exercises yet.";
            loggedExercises = Array.Empty<LoggedExercise>();
        }

        var plan = GetActiveNutritionPlan(clientId);
        IReadOnlyList<Meal> meals = plan != null
            ? this.nutritionRepository.GetMealsForPlan(plan.PlanId)
            : Array.Empty<Meal>();

        return new ClientProfileSnapshot
        {
            CaloriesSummary = $"Calories burned (all logged workouts): {totalCal}",
            LatestSessionHint = latestSessionHint,
            LoggedExercises = loggedExercises,
            Meals = meals,
        };
    }

    public NutritionPlan? GetActiveNutritionPlan(int clientId)
    {
        if (clientId <= 0)
        {
            return null;
        }

        try
        {
            var plans = this.nutritionRepository.GetNutritionPlansForClient(clientId);
            var today = DateTime.Today;
            NutritionPlan? best = null;

            foreach (var plan in plans)
            {
                if (plan.StartDate.Date > today || plan.EndDate.Date < today)
                {
                    continue;
                }

                if (best == null || plan.StartDate > best.StartDate)
                {
                    best = plan;
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading active nutrition plan: {ex.Message}");
            return null;
        }
    }

    public string BuildEstimatedWorkoutDurationDisplay(IReadOnlyList<LoggedExercise> loggedExercises)
    {
        int totalSetCount = 0;
        for (int exerciseIndex = 0; exerciseIndex < loggedExercises.Count; exerciseIndex++)
        {
            totalSetCount += loggedExercises[exerciseIndex].Sets.Count;
        }

        int totalMinutes = 0;
        if (totalSetCount > 0)
        {
            totalMinutes = totalSetCount + ((totalSetCount - 1) * 3);
        }

        var estimatedDuration = TimeSpan.FromMinutes(totalMinutes);
        return $"{(int)estimatedDuration.TotalHours:D2}:{estimatedDuration.Minutes:D2}";
    }

    public WorkoutLog BuildUpdatedWorkoutLog(WorkoutLog sourceWorkoutLog, IReadOnlyList<LoggedExercise> updatedExercises)
    {
        return new WorkoutLog
        {
            Id = sourceWorkoutLog.Id,
            ClientId = sourceWorkoutLog.ClientId,
            WorkoutName = sourceWorkoutLog.WorkoutName,
            Date = sourceWorkoutLog.Date,
            Duration = sourceWorkoutLog.Duration,
            SourceTemplateId = sourceWorkoutLog.SourceTemplateId,
            Type = sourceWorkoutLog.Type,
            TotalCaloriesBurned = sourceWorkoutLog.TotalCaloriesBurned,
            AverageMetabolicEquivalent = sourceWorkoutLog.AverageMetabolicEquivalent,
            IntensityTag = sourceWorkoutLog.IntensityTag,
            Rating = sourceWorkoutLog.Rating,
            TrainerNotes = sourceWorkoutLog.TrainerNotes,
            Exercises = new List<LoggedExercise>(updatedExercises),
        };
    }

    public List<WorkoutTemplate> GetAvailableWorkoutsForClient(int clientId)
    {
        try
        {
            return this.workoutTemplateRepository.GetAvailableWorkouts(clientId);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading workouts for client {clientId}: {exception.Message}");
            return new List<WorkoutTemplate>();
        }
    }

    public List<WorkoutTemplate> GetCustomAndTrainerAssignedWorkoutsForClient(int clientId)
    {
        var availableWorkouts = this.GetAvailableWorkoutsForClient(clientId);
        var customAndTrainerAssignedWorkouts = new List<WorkoutTemplate>();

        for (int workoutIndex = 0; workoutIndex < availableWorkouts.Count; workoutIndex++)
        {
            WorkoutTemplate availableWorkout = availableWorkouts[workoutIndex];
            bool isCustomWorkout = availableWorkout.Type == WorkoutType.CUSTOM;
            bool isTrainerAssignedWorkout = availableWorkout.Type == WorkoutType.TRAINER_ASSIGNED;

            if ((isCustomWorkout || isTrainerAssignedWorkout) && availableWorkout.ClientId == clientId)
            {
                customAndTrainerAssignedWorkouts.Add(availableWorkout);
            }
        }

        return customAndTrainerAssignedWorkouts;
    }

    public WorkoutTemplate? FindWorkoutTemplateById(int clientId, int workoutTemplateId)
    {
        var availableWorkouts = this.GetAvailableWorkoutsForClient(clientId);
        for (int workoutIndex = 0; workoutIndex < availableWorkouts.Count; workoutIndex++)
        {
            WorkoutTemplate availableWorkout = availableWorkouts[workoutIndex];
            if (availableWorkout.Id == workoutTemplateId)
            {
                return availableWorkout;
            }
        }

        return null;
    }

    public Dictionary<string, double> GetPreviousBestWeights(int clientId)
    {
        try
        {
            var allWorkoutLogs = this.workoutLogRepository.GetWorkoutHistory(clientId);
            var maximumWeightByExerciseName = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            for (int workoutLogIndex = 0; workoutLogIndex < allWorkoutLogs.Count; workoutLogIndex++)
            {
                WorkoutLog workoutLog = allWorkoutLogs[workoutLogIndex];

                for (int exerciseIndex = 0; exerciseIndex < workoutLog.Exercises.Count; exerciseIndex++)
                {
                    LoggedExercise loggedExercise = workoutLog.Exercises[exerciseIndex];

                    for (int setIndex = 0; setIndex < loggedExercise.Sets.Count; setIndex++)
                    {
                        LoggedSet loggedSet = loggedExercise.Sets[setIndex];
                        double actualWeight = loggedSet.ActualWeight ?? 0;

                        if (!maximumWeightByExerciseName.TryGetValue(loggedSet.ExerciseName, out double previousMaximumWeight) ||
                            actualWeight > previousMaximumWeight)
                        {
                            maximumWeightByExerciseName[loggedSet.ExerciseName] = actualWeight;
                        }
                    }
                }
            }

            return maximumWeightByExerciseName;
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    public List<Achievement> GetAchievements(int clientId)
    {
        try
        {
            return this.achievementsRepository
                .GetAchievementShowcaseForClient(clientId)
                .Select(achievement => new Achievement
                {
                    AchievementId = achievement.AchievementId,
                    Name = achievement.Title,
                    Description = achievement.Description,
                    Criteria = achievement.Criteria,
                    IsUnlocked = achievement.IsUnlocked,
                    Icon = achievement.IsUnlocked ? "&#xE73E;" : "&#xE72E;"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading achievements: {ex.Message}");
            return new List<Achievement>();
        }
    }

    private void ComputeCalories(WorkoutLog log)
    {
        if (log.Exercises.Count == 0 || log.Duration == TimeSpan.Zero)
        {
            return;
        }

        double weightKg = this.workoutLogRepository.GetClientWeight(log.ClientId);
        TimeSpan durationPerExercise = log.Duration / log.Exercises.Count;

        foreach (var exercise in log.Exercises)
        {
            if (exercise.MetabolicEquivalent <= 0)
            {
                exercise.MetabolicEquivalent = (float)ExerciseCalorieCalculator.GetMetabolicEquivalent(exercise.ExerciseName);
            }

            exercise.ExerciseCaloriesBurned = ExerciseCalorieCalculator.CalculateCalories(exercise.MetabolicEquivalent, weightKg, durationPerExercise);
        }

        log.TotalCaloriesBurned = log.Exercises.Sum(e => e.ExerciseCaloriesBurned);
        log.AverageMetabolicEquivalent = (float)log.Exercises.Average(e => e.MetabolicEquivalent);
        log.IntensityTag = log.AverageMetabolicEquivalent < 3.0f ? "light"
                         : log.AverageMetabolicEquivalent < 6.0f ? "moderate"
                         : "intense";
    }

    private void RunAchievementEvaluation(int clientId)
    {
        try
        {
            var newlyUnlocked = this.evaluationEngine.Evaluate(clientId);

            foreach (var title in newlyUnlocked)
            {
                var catalog = this.achievementsRepository.GetAchievementShowcaseForClient(clientId);
                var item = catalog.FirstOrDefault(
                    a => string.Equals(a.Title, title, StringComparison.OrdinalIgnoreCase));

                if (item != null)
                {
                    this.achievementBus.NotifyUnlocked(item);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ClientService] Achievement evaluation error for client {clientId}: {ex.Message}");
        }
    }

    public List<Notification> GetNotifications(int clientId)
    {
        try
        {
            return this.notificationRepository.GetNotifications(clientId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
            return new List<Notification>();
        }
    }

    public void ConfirmDeload(Notification notification)
    {
        if (notification == null)
        {
            return;
        }

        try
        {
            this.progressionService.ProcessDeloadConfirmation(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error confirming deload: {ex.Message}");
        }
    }

    public sealed class ClientProfileSnapshot
    {
        public string CaloriesSummary { get; init; } = "Calories burned (all logged workouts): 0";

        public string LatestSessionHint { get; init; } = string.Empty;

        public IReadOnlyList<LoggedExercise> LoggedExercises { get; init; } = Array.Empty<LoggedExercise>();

        public IReadOnlyList<Meal> Meals { get; init; } = Array.Empty<Meal>();
    }
}
