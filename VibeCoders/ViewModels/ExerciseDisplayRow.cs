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
    
    public class ExerciseDisplayRow
    {
        public string Name { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public List<VibeCoders.Models.LoggedSet> Sets { get; set; } = new();

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