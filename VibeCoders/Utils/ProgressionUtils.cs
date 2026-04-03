using VibeCoders.Models;

namespace VibeCoders.Utils
{
    public static class ProgressionUtils
    {
        private const double LEGS_INCREMENT = 5.0;
        private const double STANDARD_INCREMENT = 2.5;

        private const double DELOAD_FACTOR = 0.90;

        public static double CalculateRatio(int actualReps, int targetReps)
        {
            if (targetReps <= 0) return 0;
            return (double)actualReps / targetReps;
        }

        public static double DetermineWeightIncrement(MuscleGroup muscleGroup)
        {
            switch (muscleGroup)
            {
                case MuscleGroup.LEGS:
                    return 5.0;

                case MuscleGroup.CHEST:
                case MuscleGroup.BACK:
                case MuscleGroup.SHOULDERS:
                case MuscleGroup.ARMS:
                    return 2.5;

                case MuscleGroup.CORE:
                default:
                    return 2.5;
            }
        }

        public static double CalculateDeload(double currentWeight)
        {
            double raw = currentWeight * DELOAD_FACTOR;
            double rounded = Math.Round(raw * 2) / 2.0;
            return Math.Max(0, rounded);
        }
    }
}