namespace VibeCoders.Services
{
    using VibeCoders.Models;
    using VibeCoders.Utils;

    /// <summary>
    /// Service responsible for evaluating workout progression and handling plateaus.
    /// </summary>
    public class ProgressionService
    {
        private const double PlateauThreshold = 0.9;
        private const int ConsecutiveFailedSetsForPlateau = 2;

        private readonly IDataStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressionService"/> class.
        /// </summary>
        /// <param name="storage">The data storage service.</param>
        public ProgressionService(IDataStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Evaluates a completed workout log to determine if progression should be applied.
        /// </summary>
        /// <param name="workoutLog">The workout log to evaluate.</param>
        public void EvaluateWorkout(WorkoutLog workoutLog)
        {
            if (workoutLog?.Exercises == null)
            {
                return;
            }

            foreach (var exercise in workoutLog.Exercises)
            {
                this.EvaluateExercise(exercise, workoutLog.ClientId);
            }
        }

        /// <summary>
        /// Processes a confirmation for a deload recommendation based on a notification.
        /// </summary>
        /// <param name="notification">The notification containing deload details.</param>
        public void ProcessDeloadConfirmation(Notification notification)
        {
            if (notification == null)
            {
                return;
            }

            int templateExerciseId = notification.RelatedId;
            TemplateExercise? template = this.storage.GetTemplateExercise(templateExerciseId);

            if (template is null)
            {
                return;
            }

            double deloadedWeight = ProgressionUtils.CalculateDeload(template.TargetWeight);

            bool isUpdated = this.storage.UpdateTemplateWeight(template.Id, deloadedWeight);

            if (isUpdated)
            {
                notification.IsRead = true;
            }
        }

        private void EvaluateExercise(LoggedExercise exercise, int clientId)
        {
            if (exercise.Sets == null || exercise.Sets.Count == 0)
            {
                return;
            }

            int templateId = exercise.ParentTemplateExerciseId;
            TemplateExercise? template = this.storage.GetTemplateExercise(templateId);

            if (template is null)
            {
                return;
            }

            bool plateauDetected = this.CheckForPlateau(exercise, template, out double averageRatio);

            exercise.PerformanceRatio = averageRatio;

            double currentWeight = exercise.Sets[0].ActualWeight ?? template.TargetWeight;

            if (!plateauDetected && averageRatio >= 1.0)
            {
                this.ApplyProgression(exercise, template, currentWeight);
            }
            else if (plateauDetected)
            {
                this.RaisePlateauNotification(exercise, template, clientId);
            }
        }

        private bool CheckForPlateau(LoggedExercise loggedExercise, TemplateExercise template, out double averageRatio)
        {
            int consecutiveLowSets = 0;
            bool plateauDetected = false;
            double sumOfRatios = 0;

            foreach (var set in loggedExercise.Sets)
            {
                int actualReps = set.ActualReps ?? 0;
                double ratio = ProgressionUtils.CalculateRatio(actualReps, template.TargetReps);

                sumOfRatios += ratio;

                if (ratio < ProgressionService.PlateauThreshold)
                {
                    consecutiveLowSets++;
                    if (consecutiveLowSets >= ProgressionService.ConsecutiveFailedSetsForPlateau)
                    {
                        plateauDetected = true;
                    }
                }
                else
                {
                    consecutiveLowSets = 0;
                }
            }

            averageRatio = loggedExercise.Sets.Count > 0
                ? sumOfRatios / loggedExercise.Sets.Count
                : 0;

            return plateauDetected;
        }

        private void ApplyProgression(LoggedExercise exercise, TemplateExercise template, double currentWeight)
        {
            double increment = ProgressionUtils.DetermineWeightIncrement(template.MuscleGroup);
            double newWeight = currentWeight + increment;

            bool isUpdated = this.storage.UpdateTemplateWeight(template.Id, newWeight);

            if (isUpdated)
            {
                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote =
                    $"Weight increased by {increment} kg " +
                    $"({template.MuscleGroup} overload threshold met). " +
                    $"Next session target: {newWeight} kg.";
            }
        }

        private void RaisePlateauNotification(LoggedExercise exercise, TemplateExercise template, int clientId)
        {
            var notification = new Notification(
                title: "Deload Recommended",
                message: $"Plateau detected for {exercise.ExerciseName}. " +
                         $"Consider a deload: reduce weight to " +
                         $"{ProgressionUtils.CalculateDeload(template.TargetWeight)} kg next session.",
                type: NotificationType.Plateau,
                relatedId: template.Id);

            notification.ClientId = clientId;

            exercise.IsSystemAdjusted = true;
            exercise.AdjustmentNote =
                $"Plateau detected. Deload recommended: " +
                $"target weight would drop to {ProgressionUtils.CalculateDeload(template.TargetWeight)} kg.";

            this.storage.SaveNotification(notification);
        }
    }
}