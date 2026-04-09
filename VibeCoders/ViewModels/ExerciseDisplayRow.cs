namespace VibeCoders.ViewModels
{
    using System.Collections.Generic;
    using VibeCoders.Models;

    /// <summary>
    /// Represents a row of exercise data for display in a history or summary view.
    /// </summary>
    public class ExerciseDisplayRow
    {
        private const string EmptyDisplayValue = "-";

        /// <summary>
        /// Gets or sets the name of the exercise.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the muscle group targeted by the exercise.
        /// </summary>
        public string MuscleGroup { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of logged sets for this exercise.
        /// </summary>
        public List<LoggedSet> Sets { get; set; } = new List<LoggedSet>();

        /// <summary>
        /// Gets the repetition count for a specific set as a string.
        /// </summary>
        /// <param name="setIndex">The zero-based index of the set.</param>
        /// <returns>The number of repetitions, or a dash if not available.</returns>
        public string GetReps(int setIndex)
        {
            if (this.Sets == null || this.Sets.Count <= setIndex)
            {
                return ExerciseDisplayRow.EmptyDisplayValue;
            }

            return this.Sets[setIndex].ActualReps?.ToString() ?? ExerciseDisplayRow.EmptyDisplayValue;
        }

        /// <summary>
        /// Gets the weight used for a specific set as a string.
        /// </summary>
        /// <param name="setIndex">The zero-based index of the set.</param>
        /// <returns>The weight used, or a dash if not available.</returns>
        public string GetWeight(int setIndex)
        {
            if (this.Sets == null || this.Sets.Count <= setIndex)
            {
                return ExerciseDisplayRow.EmptyDisplayValue;
            }

            return this.Sets[setIndex].ActualWeight?.ToString() ?? ExerciseDisplayRow.EmptyDisplayValue;
        }
    }
}