using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class LoggedSet
    {
        public int ActReps { get; set; }
        public double Weight { get; set; } 
        public double PerformanceRatio { get; set; } 
        public int RestTimeInSeconds { get; set; } 
        public int ParentTemplateExerciseId { get; set; }
    }
}