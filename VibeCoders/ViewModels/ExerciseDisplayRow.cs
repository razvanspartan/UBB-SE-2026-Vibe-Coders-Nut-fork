using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    // Ensure this is public!
    public class ExerciseDisplayRow
    {
        public string Name { get; set; }
        public string MuscleGroup { get; set; }
        public List<VibeCoders.Models.LoggedSet> Sets { get; set; } = new();

        // ── Helper Methods ───────────────────────────────────────────────────
        // We keep these public just in case, but use the properties above for XAML
        public string GetReps(int index)
        {
            if (Sets == null || Sets.Count <= index) return "-";
            return Sets[index].ActualReps?.ToString() ?? "-";
        }

        public string GetWeight(int index)
        {
            if (Sets == null || Sets.Count <= index) return "-";
            return Sets[index].ActualWeight?.ToString() ?? "-";
        }
    }
}