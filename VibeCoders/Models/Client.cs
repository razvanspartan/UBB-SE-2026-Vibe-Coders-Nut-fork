using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class Client : User
    {
        
        public double Weight { get; set; }
        public double Height { get; set; }
        public List<WorkoutLog> WorkoutLog { get; set; } = new List<WorkoutLog>();

        public string PrimaryGoal { get; set; } = "No goal set";
        public string FormattedLastWorkout { get; set; } = "Never";

        public void SetWorkout(WorkoutLog workout)
        {
            
        }

        public void ModifyWorkout(WorkoutLog OldWorkout, WorkoutLog NewWorkout)
        {
            
        }

    }
}
