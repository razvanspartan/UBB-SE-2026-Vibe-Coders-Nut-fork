using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Repositories.Mapping
{
    internal class WorkoutTypeRepositoryMapping
    {
        public static string SerializeWorkoutType(WorkoutType type)
        {
            return type == WorkoutType.PREBUILT ? "PRE_BUILT" : type.ToString();
        }
        public static WorkoutType ParseWorkoutType(string? value)
        {
            if (string.Equals(value, "PRE_BUILT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "PREBUILT", StringComparison.OrdinalIgnoreCase))
            {
                return WorkoutType.PREBUILT;
            }

            if (string.Equals(value, "TRAINERASSIGNED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "TRAINER-ASSIGNED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "TRAINER ASSIGNED", StringComparison.OrdinalIgnoreCase))
            {
                return WorkoutType.TRAINER_ASSIGNED;
            }

            return Enum.TryParse<WorkoutType>(value, true, out var parsed)
                ? parsed
                : WorkoutType.CUSTOM;
        }
    }
}
