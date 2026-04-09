namespace VibeCoders.Services
{
    using System;
    using System.Collections.Generic;
    using VibeCoders.Models;

    public class TrainerService
    {
        private readonly IDataStorage storage;

        public TrainerService(IDataStorage storage)
        {
            this.storage = storage;
        }

        public List<Client> GetAssignedClients(int trainerId)
        {
            return this.storage.GetTrainerClient(trainerId);
        }

        public List<WorkoutLog> GetClientWorkoutHistory(int clientId)
        {
            return this.storage.GetWorkoutHistory(clientId);
        }

        public bool SaveWorkoutFeedback(WorkoutLog workoutLog)
        {
            if (workoutLog == null)
            {
                return false;
            }

            return this.storage.UpdateWorkoutLogFeedback(workoutLog.Id, workoutLog.Rating, workoutLog.TrainerNotes);
        }

        public void AssignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }

        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            if (template == null)
            {
                return false;
            }

            return this.storage.SaveTrainerWorkout(template);
        }

        public List<string> GetPredefinedExercises()
        {
            return this.storage.GetAllExerciseNames();
        }

        public bool AssignNutritionPlan(NutritionPlan plan, int clientId)
        {
            if (plan == null)
            {
                return false;
            }

            int minimumValidClientId = 0;
            if (clientId <= minimumValidClientId)
            {
                return false;
            }

            this.storage.SaveNutritionPlanForClient(plan, clientId);
            return true;
        }
    }
}