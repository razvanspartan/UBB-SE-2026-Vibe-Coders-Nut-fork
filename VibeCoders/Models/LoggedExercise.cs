using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class LoggedExercise
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; }
        public int WorkoutLogId { get; set; }
        public List<LoggedSet> Sets { get; set; } = new List<LoggedSet>();
        public double PerformanceRatio { get; set; }
        public bool IsSystemAdjusted { get; set; }
        public string AdjustmentNote { get; set; }
    }
}
