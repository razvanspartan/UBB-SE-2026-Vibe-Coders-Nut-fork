using VibeCoders.Models;

namespace VibeCoders.Utils
{
    /// <summary>
    /// Pure static helpers used by <see cref="Services.ProgressionService"/>.
    /// No side effects, no dependencies — easy to unit test.
    /// </summary>
    public static class ProgressionUtils
    {
        // Weight increment constants (kg) per requirements:
        // Legs: 5kg, Chest/Back/Shoulders/Arms: 2.5kg
        private const double LEGS_INCREMENT = 5.0;
        private const double STANDARD_INCREMENT = 2.5;

        // Deload factor: reduce current weight by 10 % when a plateau is confirmed.
        private const double DELOAD_FACTOR = 0.90;

        /// <summary>
        /// Calculates the performance ratio for a single set.
        /// ratio = actualReps / targetReps
        /// ≥ 1.0 → the client hit or exceeded the target (overload is possible).
        /// &lt; 0.9 → the client struggled (potential plateau signal).
        /// </summary>
        /// <param name="actualReps">Reps the client actually performed.</param>
        /// <param name="targetReps">Reps prescribed by the template.</param>
        /// <returns>
        /// A non-negative ratio. Returns 0 when <paramref name="targetReps"/> is
        /// zero or negative to avoid division-by-zero.
        /// </returns>
        public static double CalculateRatio(int actualReps, int targetReps)
        {
            if (targetReps <= 0) return 0;
            return (double)actualReps / targetReps;
        }

        /// <summary>
        /// Returns the weight increment (in kg) to apply after a successful overload,
        /// based on the primary muscle group of the exercise.
        /// </summary>
        /// <param name="muscleGroup">The <see cref="MuscleGroup"/> of the exercise.</param>
        /// <returns>2.5 kg for large muscle groups, 1.25 kg for smaller ones.</returns>
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

        /// <summary>
        /// Calculates the deloaded weight after a plateau is confirmed.
        /// Reduces the current weight by <see cref="DELOAD_FACTOR"/> (10 %).
        /// The result is rounded to the nearest 0.25 kg to match standard plate increments.
        /// </summary>
        /// <param name="currentWeight">The current prescribed weight in kg.</param>
        /// <returns>The new deloaded weight, minimum 0.</returns>
        public static double CalculateDeload(double currentWeight)
        {
            double raw = currentWeight * DELOAD_FACTOR;
            // Round to nearest 0.5 kg per requirements
            double rounded = Math.Round(raw * 2) / 2.0;
            return Math.Max(0, rounded);
        }
    }
}