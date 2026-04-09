namespace VibeCoders.Utils
{
    using System;
    using VibeCoders.Models;

    /// <summary>
    /// Utility class for progression-related calculations, such as weight increments and deloading.
    /// </summary>
    public static class ProgressionUtils
    {
        private const double LegsIncrement = 5.0;
        private const double StandardIncrement = 2.5;
        private const double DeloadFactor = 0.90;
        private const double RoundingMultiplier = 2.0;
        private const double MinimumWeight = 0.0;
        private const int DefaultRatioResult = 0;

        /// <summary>
        /// Calculates the ratio of actual repetitions to target repetitions.
        /// </summary>
        /// <param name="actualReps">The number of repetitions actually performed.</param>
        /// <param name="targetReps">The number of repetitions targeted.</param>
        /// <returns>The performance ratio.</returns>
        public static double CalculateRatio(int actualReps, int targetReps)
        {
            if (targetReps <= 0)
            {
                return ProgressionUtils.DefaultRatioResult;
            }

            return (double)actualReps / targetReps;
        }

        /// <summary>
        /// Determines the appropriate weight increment based on the muscle group.
        /// </summary>
        /// <param name="muscleGroup">The muscle group targeted by the exercise.</param>
        /// <returns>The weight increment in kilograms.</returns>
        public static double DetermineWeightIncrement(MuscleGroup muscleGroup)
        {
            switch (muscleGroup)
            {
                case MuscleGroup.LEGS:
                    return ProgressionUtils.LegsIncrement;

                case MuscleGroup.CHEST:
                case MuscleGroup.BACK:
                case MuscleGroup.SHOULDERS:
                case MuscleGroup.ARMS:
                    return ProgressionUtils.StandardIncrement;

                case MuscleGroup.CORE:
                default:
                    return ProgressionUtils.StandardIncrement;
            }
        }

        /// <summary>
        /// Calculates the deloaded weight, typically 90% of the current weight, rounded to the nearest 0.5kg.
        /// </summary>
        /// <param name="currentWeight">The current weight.</param>
        /// <returns>The deloaded weight.</returns>
        public static double CalculateDeload(double currentWeight)
        {
            double rawDeloadedWeight = currentWeight * ProgressionUtils.DeloadFactor;

            double roundedWeight = Math.Round(rawDeloadedWeight * ProgressionUtils.RoundingMultiplier) / ProgressionUtils.RoundingMultiplier;

            return Math.Max(ProgressionUtils.MinimumWeight, roundedWeight);
        }
    }
}