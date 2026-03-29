using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Utils;

namespace VibeCoders.Services
{
    public class ProgressionService
    {
        private readonly IDataStorage _storage;

        public ProgressionService(IDataStorage storage)
        {
            _storage = storage;
        }

        public void EvaluateWorkout(WorkoutLog log)
        {
            if (log.Exercises == null) return;

            foreach (var exercise in log.Exercises)
            {
                EvaluateExercise(exercise);
            }
        }

        private void EvaluateExercise(LoggedExercise exercise)
        {
            if (exercise.Sets == null || exercise.Sets.Count == 0) return;

            int templateId = exercise.Sets[0].ParentTemplateExerciseId;
            TemplateExercise template = _storage.GetTemplateExercise(templateId);

            if (template == null) return;

            bool isOverloadPossible = true;
            bool plateauDetected = false;
            int consecutiveLowSets = 0;
            double currentWeight = exercise.Sets[0].Weight;
            double sumOfRatios = 0;

            foreach (var set in exercise.Sets)
            {
                double ratio = ProgressionUtils.CalculateRatio(set.ActReps, template.TargetReps);

                set.PerformanceRatio = ratio;
                sumOfRatios += ratio;

                if (ratio < 1.0) isOverloadPossible = false;

                if (ratio < 0.9)
                {
                    consecutiveLowSets++;
                    if (consecutiveLowSets >= 2) plateauDetected = true;
                }
                else
                {
                    consecutiveLowSets = 0;
                }
            }

            exercise.PerformanceRatio = sumOfRatios / exercise.Sets.Count;

            if (isOverloadPossible)
            {
                double increment = ProgressionUtils.DetermineWeightIncrement(template.MuscleGroup);
                double newWeight = currentWeight + increment;
                _storage.UpdateTemplateWeight(template.Id, newWeight);

                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote = $"Overload: {currentWeight}kg -> {newWeight}kg (Avg Ratio: {exercise.PerformanceRatio:F2})";
            }
            else if (plateauDetected)
            {
                var notification = new Notification(
                    "Deload Recommended",
                    $"Plateau detected for {exercise.ExerciseName}.",
                    NotificationType.PLATEAU,
                    template.Id
                );

                _storage.SaveNotification(notification);
                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote = "Plateau detected. Deload recommendation sent to dashboard.";
            }
        }
    }
}