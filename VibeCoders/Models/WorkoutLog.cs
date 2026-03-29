using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class WorkoutLog
    {
        public int Id { get; set; }
        public string WorkoutName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        public int SourceTemplateId { get; set; }
        public List<LoggedExercise> Exercises { get; set; } = new List<LoggedExercise>();
    }
}
