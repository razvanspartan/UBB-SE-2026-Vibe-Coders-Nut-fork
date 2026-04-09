namespace VibeCoders.Utils
{
    using System;
    using VibeCoders.Models;

    public static class ProgressionUtils
    {
        private const double LegsIncrement = 5.0;
        private const double StandardIncrement = 2.5;
        private const double DeloadFactor = 0.90;
        private const double RoundingMultiplier = 2.0;
        private const double MinimumWeight = 0.0;
        private const int DefaultRatioResult = 0;

        public static double CalculateRatio(int actualReps, int targetReps)
        {
            if (targetReps <= 0)
            {
                return ProgressionUtils.DefaultRatioResult;
            }

            return (double)actualReps / targetReps;
        }

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

        public static double CalculateDeload(double currentWeight)
        {
            double rawDeloadedWeight = currentWeight * ProgressionUtils.DeloadFactor;

            double roundedWeight = Math.Round(rawDeloadedWeight * ProgressionUtils.RoundingMultiplier) / ProgressionUtils.RoundingMultiplier;

            return Math.Max(ProgressionUtils.MinimumWeight, roundedWeight);
        }
    }
}