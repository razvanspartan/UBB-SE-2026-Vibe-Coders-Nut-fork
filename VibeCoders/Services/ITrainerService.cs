using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VibeCoders.Models;

namespace VibeCoders.Services
{
    public interface ITrainerService
    {
        List<Client> GetAssignedClients(int trainerId);

        List<WorkoutLog> GetClientWorkoutHistory(int clientId);

        bool SaveWorkoutFeedback(WorkoutLog log);

        void AssignWorkout(Client client, WorkoutLog workout);

        bool SaveTrainerWorkout(WorkoutTemplate template);

        List<string> GetPredefinedExercises();

        bool AssignNutritionPlan(NutritionPlan plan, int clientId);
    }
}