using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Integration;

namespace VibeCoders.Services
{
    public class ClientService
    {
        private readonly IDataStorage _storage;
        private readonly ProgressionService _progressionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EvaluationEngine _evaluationEngine;
        private readonly IAchievementUnlockedBus _achievementBus;

        public ClientService(
            IDataStorage storage,
            ProgressionService progressionService,
            IHttpClientFactory httpClientFactory,
            EvaluationEngine evaluationEngine,
            IAchievementUnlockedBus achievementBus
            )
        {
            _storage            = storage;
            _progressionService = progressionService;
            _httpClientFactory  = httpClientFactory;
            _evaluationEngine   = evaluationEngine;
            _achievementBus     = achievementBus;
        }

        private const double DefaultMet = 5.0;

        public bool FinalizeWorkout(WorkoutLog log)
        {
            if (log == null || log.Exercises == null) return false;

            try
            {
                log.Date = DateTime.Now;
                _progressionService.EvaluateWorkout(log);

                ComputeCalories(log);

                bool isSaved = _storage.SaveWorkoutLog(log);
                if (!isSaved) return false;

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
                return false;

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
            if (updatedLog == null) return false;

            try
            {
                return _storage.SaveWorkoutLog(updatedLog);
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
            return true;
        }

        public NutritionSyncPayload BuildNutritionSyncPayload(int clientId)
        {
            var history = _storage.GetWorkoutHistory(clientId);
            var totalCalories = history.Sum(h => h.TotalCaloriesBurned);
            var last = history.FirstOrDefault();
            var difficulty = string.IsNullOrWhiteSpace(last?.IntensityTag) ? "unknown" : last.IntensityTag;

            float bmi = 0f;
            List<Client> roster;
            try
            {
                roster = _storage.GetTrainerClient(1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClientService: BMI lookup failed; sync continues with UserBmi=0. {ex.Message}");
                roster = [];
            }

            var profileClient = roster.FirstOrDefault(c => c.Id == clientId);
            if (profileClient is { Weight: > 0, Height: > 0 })
            {
                bmi = (float)BmiCalculator.Calculate(profileClient.Weight, profileClient.Height);
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
            var history = _storage.GetWorkoutHistory(clientId);
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
                ? _storage.GetMealsForPlan(plan.PlanId)
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
                return null;

            try
            {
                var plans = _storage.GetNutritionPlansForClient(clientId);
                var today = DateTime.Today;
                NutritionPlan? best = null;

                foreach (var plan in plans)
                {
                    if (plan.StartDate.Date > today || plan.EndDate.Date < today)
                        continue;

                    if (best == null || plan.StartDate > best.StartDate)
                        best = plan;
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
                AverageMet = sourceWorkoutLog.AverageMet,
                IntensityTag = sourceWorkoutLog.IntensityTag,
                Rating = sourceWorkoutLog.Rating,
                TrainerNotes = sourceWorkoutLog.TrainerNotes,
                Exercises = new List<LoggedExercise>(updatedExercises),
            };
        }

        private void ComputeCalories(WorkoutLog log)
        {
            if (log.Exercises.Count == 0 || log.Duration == TimeSpan.Zero) return;

            double weightKg = _storage.GetClientWeight(log.ClientId);
            TimeSpan durationPerExercise = log.Duration / log.Exercises.Count;

            foreach (var exercise in log.Exercises)
            {
                if (exercise.Met <= 0)
                    exercise.Met = (float)ExerciseCalorieCalculator.GetMet(exercise.ExerciseName);

                exercise.ExerciseCaloriesBurned = ExerciseCalorieCalculator.Calculate(exercise.Met, weightKg, durationPerExercise);
            }

            log.TotalCaloriesBurned = log.Exercises.Sum(e => e.ExerciseCaloriesBurned);
            log.AverageMet = (float)log.Exercises.Average(e => e.Met);
            log.IntensityTag = log.AverageMet < 3.0f ? "light"
                             : log.AverageMet < 6.0f ? "moderate"
                             : "intense";
        }

        private void RunAchievementEvaluation(int clientId)
        {
            try
            {
                var newlyUnlocked = _evaluationEngine.Evaluate(clientId);

                foreach (var title in newlyUnlocked)
                {
                    var catalog = _storage.GetAchievementShowcaseForClient(clientId);
                    var item    = catalog.FirstOrDefault(
                        a => string.Equals(a.Title, title, StringComparison.OrdinalIgnoreCase));

                    if (item != null)
                        _achievementBus.NotifyUnlocked(item);
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
                return _storage.GetNotifications(clientId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
                return new List<Notification>();
            }
        }

        public void ConfirmDeload(Notification notification)
        {
            if (notification == null) return;

            try
            {
                _progressionService.ProcessDeloadConfirmation(notification);
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
}
