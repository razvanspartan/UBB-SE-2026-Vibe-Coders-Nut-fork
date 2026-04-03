using VibeCoders.Models;
using VibeCoders.Utils;

namespace VibeCoders.Services
{
    public class ProgressionService
    {
        private readonly IDataStorage _storage;

        private const double PLATEAU_THRESHOLD = 0.9;

        private const int CONSECUTIVE_FAILED_SETS_FOR_PLATEAU = 2;

        public ProgressionService(IDataStorage storage)
        {
            _storage = storage;
        }

        public void EvaluateWorkout(WorkoutLog log)
        {
            if (log?.Exercises == null) return;

            foreach (var exercise in log.Exercises)
            {
                EvaluateExercise(exercise, log.ClientId);
            }
        }

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
                notification.IsRead = true;
            }
        }

        private void EvaluateExercise(LoggedExercise exercise, int clientId)
        {
            if (exercise.Sets == null || exercise.Sets.Count == 0) return;

            int templateId = exercise.ParentTemplateExerciseId;
            TemplateExercise? template = _storage.GetTemplateExercise(templateId);

            if (template is null) return;

            bool plateauDetected = CheckForPlateau(exercise, template, out double avgRatio);

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