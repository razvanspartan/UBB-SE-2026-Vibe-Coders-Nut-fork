using VibeCoders.Models;
using VibeCoders.Utils;

namespace VibeCoders.Services
{
    /// <summary>
    /// Evaluates a completed <see cref="WorkoutLog"/> and applies progressive
    /// overload logic: increments template weights on success, detects plateaus,
    /// and processes deload confirmations.
    /// </summary>
    public class ProgressionService
    {
        private readonly IDataStorage _storage;

        // A set is considered "failed" when the client hits less than 90 % of target reps.
        private const double PLATEAU_THRESHOLD = 0.9;

        // Two consecutive failed sets within a session trigger a plateau notification.
        private const int CONSECUTIVE_FAILED_SETS_FOR_PLATEAU = 2;

        public ProgressionService(IDataStorage storage)
        {
            _storage = storage;
        }

        // ── Public entry point ───────────────────────────────────────────────

        /// <summary>
        /// Main loop: iterates every exercise in the log and evaluates it.
        /// Called by <see cref="ClientService.FinalizeWorkout"/> after a session ends.
        /// </summary>
        public void EvaluateWorkout(WorkoutLog log)
        {
            if (log?.Exercises == null) return;

            foreach (var exercise in log.Exercises)
            {
                EvaluateExercise(exercise, log.ClientId);
            }
        }

        /// <summary>
        /// Called when the client confirms they want to deload an exercise.
        /// Reduces the template weight by the deload factor and marks the
        /// exercise as system-adjusted.
        /// </summary>
        public void ProcessDeloadConfirmation(Notification notification)
        {
            if (notification == null) return;

            int templateExerciseId = notification.RelatedId;
            TemplateExercise? template = _storage.GetTemplateExercise(templateExerciseId);

            if (template is null) return;

            double deloadedWeight = ProgressionUtils.CalculateDeload(template.TargetWeight);

            bool updated = _storage.UpdateTemplateWeight(template.Id, deloadedWeight);

            if (updated)
            {
                // Mark the notification as read so the UI dismisses it.
                notification.IsRead = true;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Evaluates a single exercise:
        ///   1. Fetches its template for target values.
        ///   2. Calculates per-set performance ratios.
        ///   3. Either applies progression or raises a plateau notification.
        /// </summary>
        private void EvaluateExercise(LoggedExercise exercise, int clientId)
        {
            if (exercise.Sets == null || exercise.Sets.Count == 0) return;

            int templateId = exercise.ParentTemplateExerciseId;
            TemplateExercise? template = _storage.GetTemplateExercise(templateId);

            if (template is null) return;

            bool plateauDetected = CheckForPlateau(exercise, template, out double avgRatio);

            // Store the average ratio on the exercise for badge/tooltip rendering.
            exercise.PerformanceRatio = avgRatio;

            double currentWeight = exercise.Sets[0].ActualWeight ?? template.TargetWeight;

            if (!plateauDetected && avgRatio >= 1.0)
            {
                ApplyProgression(exercise, template, currentWeight);
            }
            else if (plateauDetected)
            {
                RaisePlateauNotification(exercise, template, clientId);
            }
        }

        /// <summary>
        /// Checks whether two or more consecutive sets fell below the plateau
        /// threshold. Also computes the average performance ratio across all sets.
        /// </summary>
        /// <param name="exercise">The exercise to inspect.</param>
        /// <param name="template">The template supplying target reps.</param>
        /// <param name="avgRatio">Out: average ratio across all sets.</param>
        /// <returns>True when a plateau is detected.</returns>
        private bool CheckForPlateau(LoggedExercise exercise, TemplateExercise template, out double avgRatio)
        {
            int consecutiveLowSets = 0;
            bool plateauDetected = false;
            double sumOfRatios = 0;

            foreach (var set in exercise.Sets)
            {
                int actualReps = set.ActualReps ?? 0;
                double ratio = ProgressionUtils.CalculateRatio(actualReps, template.TargetReps);

                sumOfRatios += ratio;

                if (ratio < PLATEAU_THRESHOLD)
                {
                    consecutiveLowSets++;
                    if (consecutiveLowSets >= CONSECUTIVE_FAILED_SETS_FOR_PLATEAU)
                    {
                        plateauDetected = true;
                    }
                }
                else
                {
                    consecutiveLowSets = 0;
                }
            }

            avgRatio = exercise.Sets.Count > 0
                ? sumOfRatios / exercise.Sets.Count
                : 0;

            return plateauDetected;
        }

        /// <summary>
        /// Increments the template weight, persists the change, and annotates
        /// the exercise so the UI can show the "System Adjusted" badge.
        /// </summary>
        private void ApplyProgression(LoggedExercise exercise, TemplateExercise template, double currentWeight)
        {
            double increment = ProgressionUtils.DetermineWeightIncrement(template.MuscleGroup);
            double newWeight = currentWeight + increment;

            bool updated = _storage.UpdateTemplateWeight(template.Id, newWeight);

            if (updated)
            {
                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote =
                    $"Weight increased by {increment} kg " +
                    $"({template.MuscleGroup} overload threshold met). " +
                    $"Next session target: {newWeight} kg.";
            }
        }

        /// <summary>
        /// Creates and persists a plateau/deload notification.
        /// The client will see this on their dashboard and can confirm a deload.
        /// </summary>
        private void RaisePlateauNotification(LoggedExercise exercise, TemplateExercise template, int clientId)
        {
            var notification = new Notification(
                title: "Deload Recommended",
                message: $"Plateau detected for {exercise.ExerciseName}. " +
                         $"Consider a deload: reduce weight to " +
                         $"{ProgressionUtils.CalculateDeload(template.TargetWeight)} kg next session.",
                type: NotificationType.Plateau,
                relatedId: template.Id
            );

            notification.ClientId = clientId;

            exercise.IsSystemAdjusted = true;
            exercise.AdjustmentNote =
                $"Plateau detected. Deload recommended: " +
                $"target weight would drop to {ProgressionUtils.CalculateDeload(template.TargetWeight)} kg.";

            _storage.SaveNotification(notification);
        }
    }
}