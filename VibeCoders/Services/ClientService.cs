#pragma warning disable IL2026

namespace VibeCoders.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using VibeCoders.Domain;
    using VibeCoders.Models;
    using VibeCoders.Models.Integration;

    /// <summary>
    /// Provides services for managing client-related data and operations, including workouts and nutrition.
    /// </summary>
    public class ClientService
    {
        private const double DefaultMetValue = 5.0;
        private const float LightIntensityThreshold = 3.0f;
        private const float ModerateIntensityThreshold = 6.0f;
        private const string LightIntensityTag = "light";
        private const string ModerateIntensityTag = "moderate";
        private const string IntenseIntensityTag = "intense";

        private readonly IDataStorage storage;
        private readonly ProgressionService progressionService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly EvaluationEngine evaluationEngine;
        private readonly IAchievementUnlockedBus achievementBus;
        private readonly NutritionSyncOptions nutritionSyncOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientService"/> class.
        /// </summary>
        /// <param name="storage">The data storage service.</param>
        /// <param name="progressionService">The progression evaluation service.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="evaluationEngine">The achievement evaluation engine.</param>
        /// <param name="achievementBus">The achievement notification bus.</param>
        /// <param name="nutritionSyncOptions">Options for nutrition synchronization.</param>
        public ClientService(
            IDataStorage storage,
            ProgressionService progressionService,
            IHttpClientFactory httpClientFactory,
            EvaluationEngine evaluationEngine,
            IAchievementUnlockedBus achievementBus,
            NutritionSyncOptions nutritionSyncOptions)
        {
            this.storage = storage;
            this.progressionService = progressionService;
            this.httpClientFactory = httpClientFactory;
            this.evaluationEngine = evaluationEngine;
            this.achievementBus = achievementBus;
            this.nutritionSyncOptions = nutritionSyncOptions;
        }

        /// <summary>
        /// Finalizes a workout by calculating meta-data and saving it to storage.
        /// </summary>
        /// <param name="workoutLog">The workout log to finalize.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool FinalizeWorkout(WorkoutLog workoutLog)
        {
            if (workoutLog == null || workoutLog.Exercises == null)
            {
                return false;
            }

            try
            {
                workoutLog.Date = DateTime.Now;
                this.progressionService.EvaluateWorkout(workoutLog);

                this.ComputeCalories(workoutLog);

                bool isSaved = this.storage.SaveWorkoutLog(workoutLog);
                if (!isSaved)
                {
                    return false;
                }

                this.RunAchievementEvaluation(workoutLog.ClientId);

                return true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error finalizing workout: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves a set for a specific exercise in a workout log.
        /// </summary>
        /// <param name="workoutLog">The workout log containing the exercise.</param>
        /// <param name="exerciseName">The name of the exercise.</param>
        /// <param name="loggedSet">The set to save.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SaveSet(WorkoutLog workoutLog, string exerciseName, LoggedSet loggedSet)
        {
            if (workoutLog == null || loggedSet == null || string.IsNullOrWhiteSpace(exerciseName))
            {
                return false;
            }

            try
            {
                var exerciseToUpdate = workoutLog.Exercises
                    .FirstOrDefault(exercise => exercise.ExerciseName == exerciseName);

                if (exerciseToUpdate == null)
                {
                    exerciseToUpdate = new LoggedExercise
                    {
                        ExerciseName = exerciseName,
                        WorkoutLogId = workoutLog.Id
                    };
                    workoutLog.Exercises.Add(exerciseToUpdate);
                }

                loggedSet.SetIndex = exerciseToUpdate.Sets.Count + 1;
                loggedSet.WorkoutLogId = workoutLog.Id;
                loggedSet.ExerciseName = exerciseName;
                exerciseToUpdate.Sets.Add(loggedSet);

                return true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving set: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Modifies an existing workout log.
        /// </summary>
        /// <param name="updatedWorkoutLog">The updated workout log data.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public bool ModifyWorkout(WorkoutLog updatedWorkoutLog)
        {
            if (updatedWorkoutLog == null)
            {
                return false;
            }

            try
            {
                return this.storage.SaveWorkoutLog(updatedWorkoutLog);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying workout: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Synchronizes nutrition data with a remote endpoint.
        /// </summary>
        /// <param name="nutritionSyncPayload">The data to synchronize.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing true if successful.</returns>
        public async Task<bool> SyncNutritionAsync(
            NutritionSyncPayload nutritionSyncPayload,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var httpClient = this.httpClientFactory.CreateClient();
                var responseMessage = await httpClient
                    .PostAsJsonAsync(this.nutritionSyncOptions.Endpoint, nutritionSyncPayload, cancellationToken)
                    .ConfigureAwait(false);

                return responseMessage.IsSuccessStatusCode;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing nutrition: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the currently active nutrition plan for a client.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
        /// <returns>The active nutrition plan if found; otherwise, null.</returns>
        public NutritionPlan? GetActiveNutritionPlan(int clientId)
        {
            if (clientId <= 0)
            {
                return null;
            }

            try
            {
                var nutritionPlans = this.storage.GetNutritionPlansForClient(clientId);
                var todayDate = DateTime.Today;
                NutritionPlan? bestNutritionPlan = null;

                foreach (var nutritionPlan in nutritionPlans)
                {
                    if (nutritionPlan.StartDate.Date > todayDate || nutritionPlan.EndDate.Date < todayDate)
                    {
                        continue;
                    }

                    if (bestNutritionPlan == null || nutritionPlan.StartDate > bestNutritionPlan.StartDate)
                    {
                        bestNutritionPlan = nutritionPlan;
                    }
                }

                return bestNutritionPlan;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active nutrition plan: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves notifications for a specific client.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
        /// <returns>A list of notifications.</returns>
        public List<Notification> GetNotifications(int clientId)
        {
            try
            {
                return this.storage.GetNotifications(clientId);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notifications: {exception.Message}");
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Processes a confirmation for a deload recommendation.
        /// </summary>
        /// <param name="deloadNotification">The notification representing the deload recommendation.</param>
        public void ConfirmDeload(Notification deloadNotification)
        {
            if (deloadNotification == null)
            {
                return;
            }

            try
            {
                this.progressionService.ProcessDeloadConfirmation(deloadNotification);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error confirming deload: {exception.Message}");
            }
        }

        private void ComputeCalories(WorkoutLog workoutLog)
        {
            if (workoutLog.Exercises.Count == 0 || workoutLog.Duration == TimeSpan.Zero)
            {
                return;
            }

            double weightInKilograms = this.storage.GetClientWeight(workoutLog.ClientId);
            TimeSpan durationPerExercise = workoutLog.Duration / workoutLog.Exercises.Count;

            foreach (var loggedExercise in workoutLog.Exercises)
            {
                if (loggedExercise.Met <= 0)
                {
                    loggedExercise.Met = (float)ExerciseCalorieCalculator.GetMet(loggedExercise.ExerciseName);
                }

                loggedExercise.ExerciseCaloriesBurned = ExerciseCalorieCalculator.Calculate(loggedExercise.Met, weightInKilograms, durationPerExercise);
            }

            workoutLog.TotalCaloriesBurned = workoutLog.Exercises.Sum(exercise => exercise.ExerciseCaloriesBurned);
            workoutLog.AverageMet = (float)workoutLog.Exercises.Average(exercise => exercise.Met);
            workoutLog.IntensityTag = workoutLog.AverageMet < ClientService.LightIntensityThreshold ? ClientService.LightIntensityTag
                             : workoutLog.AverageMet < ClientService.ModerateIntensityThreshold ? ClientService.ModerateIntensityTag
                             : ClientService.IntenseIntensityTag;
        }

        private void RunAchievementEvaluation(int clientId)
        {
            try
            {
                var newlyUnlockedTitles = this.evaluationEngine.Evaluate(clientId);

                foreach (var achievementTitle in newlyUnlockedTitles)
                {
                    var achievementCatalog = this.storage.GetAchievementShowcaseForClient(clientId);
                    var unlockedItem = achievementCatalog.FirstOrDefault(
                        achievement => string.Equals(achievement.Title, achievementTitle, StringComparison.OrdinalIgnoreCase));

                    if (unlockedItem != null)
                    {
                        this.achievementBus.NotifyUnlocked(unlockedItem);
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ClientService] Achievement evaluation error for client {clientId}: {exception.Message}");
            }
        }
    }
}
