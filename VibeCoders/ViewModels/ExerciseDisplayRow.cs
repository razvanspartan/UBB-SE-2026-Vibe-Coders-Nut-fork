namespace VibeCoders.ViewModels
{
    using System.Collections.Generic;
    using VibeCoders.Models;

    public class ExerciseDisplayRow
    {
        private const string EmptyDisplayValue = "-";

        public string Name { get; set; } = string.Empty;

        public string MuscleGroup { get; set; } = string.Empty;

        public List<LoggedSet> Sets { get; set; } = new List<LoggedSet>();

        public string GetReps(int setIndex)
        {
            if (this.Sets == null || this.Sets.Count <= setIndex)
            {
                return ExerciseDisplayRow.EmptyDisplayValue;
            }

            return this.Sets[setIndex].ActualReps?.ToString() ?? ExerciseDisplayRow.EmptyDisplayValue;
        }

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