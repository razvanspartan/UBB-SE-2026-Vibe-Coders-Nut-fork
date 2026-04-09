using System.Net.Http;
using System.Net.Http.Json;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Integration;

namespace VibeCoders.Services
{
    public class ClientService
    {
        private readonly IDataStorage storage;
        private readonly ProgressionService _progressionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EvaluationEngine _evaluationEngine;
        private readonly IAchievementUnlockedBus _achievementBus;
        private readonly NutritionSyncOptions _nutritionSync;

        public ClientService(
            IDataStorage storage,
            ProgressionService progressionService,
            IHttpClientFactory httpClientFactory,
            EvaluationEngine evaluationEngine,
            IAchievementUnlockedBus achievementBus,
            NutritionSyncOptions nutritionSync)
        {
            this.storage            = storage;
            _progressionService = progressionService;
            _httpClientFactory  = httpClientFactory;
            _evaluationEngine   = evaluationEngine;
            _achievementBus     = achievementBus;
            _nutritionSync      = nutritionSync;
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

                bool isSaved = storage.SaveWorkoutLog(log);
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
                return storage.SaveWorkoutLog(updatedLog);
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
                var client = _httpClientFactory.CreateClient();
                var response = await client
                    .PostAsJsonAsync(_nutritionSync.Endpoint, payload, cancellationToken)
                    .ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing nutrition: {ex.Message}");
                return false;
            }
        }

        public NutritionPlan? GetActiveNutritionPlan(int clientId)
        {
            if (clientId <= 0)
                return null;

            try
            {
                var plans = storage.GetNutritionPlansForClient(clientId);
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

        private void ComputeCalories(WorkoutLog log)
        {
            if (log.Exercises.Count == 0 || log.Duration == TimeSpan.Zero) return;

            double weightKg = storage.GetClientWeight(log.ClientId);
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
                    var catalog = storage.GetAchievementShowcaseForClient(clientId);
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
                return storage.GetNotifications(clientId);
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
    }
}
