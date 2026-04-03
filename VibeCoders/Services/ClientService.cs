using System.Net.Http;
using System.Net.Http.Json;
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

        private const string NutritionApiEndpoint = "https://nutrition-app.vibecoders.internal/api/nutrition/sync";

        public ClientService(
            IDataStorage storage,
            ProgressionService progressionService,
            IHttpClientFactory httpClientFactory,
            EvaluationEngine evaluationEngine,
            IAchievementUnlockedBus achievementBus)
        {
            _storage            = storage;
            _progressionService = progressionService;
            _httpClientFactory  = httpClientFactory;
            _evaluationEngine   = evaluationEngine;
            _achievementBus     = achievementBus;
        }

        public bool FinalizeWorkout(WorkoutLog log)
        {
            if (log == null || log.Exercises == null) return false;

            try
            {
                log.Date = DateTime.Now;
                _progressionService.EvaluateWorkout(log);

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

                set.SetIndex = exercise.Sets.Count;
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
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client
                    .PostAsJsonAsync(NutritionApiEndpoint, payload, cancellationToken)
                    .ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing nutrition: {ex.Message}");
                return false;
            }
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
    }
}
