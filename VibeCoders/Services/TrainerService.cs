namespace VibeCoders.Services
{
    using System;
    using System.Collections.Generic;
    using VibeCoders.Models;

    /// <summary>
    /// Service responsible for trainer operations, including managing client history, feedback, and nutrition plans.
    /// </summary>
    public class TrainerService
    {
        private readonly IDataStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainerService"/> class.
        /// </summary>
        /// <param name="storage">The data storage service.</param>
        public TrainerService(IDataStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Retrieves the list of clients assigned to a specific trainer.
        /// </summary>
        /// <param name="trainerId">The trainer's ID.</param>
        /// <returns>A list of assigned clients.</returns>
        public List<Client> GetAssignedClients(int trainerId)
        {
            return this.storage.GetTrainerClient(trainerId);
        }

        /// <summary>
        /// Retrieves the workout history for a specific client.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
        /// <returns>A list of workout logs.</returns>
        public List<WorkoutLog> GetClientWorkoutHistory(int clientId)
        {
            return this.storage.GetWorkoutHistory(clientId);
        }

        /// <summary>
        /// Saves feedback (rating and notes) from a trainer for a completed workout.
        /// </summary>
        /// <param name="workoutLog">The workout log containing feedback details.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SaveWorkoutFeedback(WorkoutLog workoutLog)
        {
            if (workoutLog == null)
            {
                return false;
            }

            return this.storage.UpdateWorkoutLogFeedback(workoutLog.Id, workoutLog.Rating, workoutLog.TrainerNotes);
        }

        /// <summary>
        /// Assigns a workout to a client. (Currently not implemented).
        /// </summary>
        /// <param name="client">The client to assign to.</param>
        /// <param name="workout">The workout to assign.</param>
        public void AssignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }

        /// <summary>
        /// Saves a workout template created by a trainer.
        /// </summary>
        /// <param name="template">The workout template to save.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            if (template == null)
            {
                return false;
            }

            return this.storage.SaveTrainerWorkout(template);
        }

        /// <summary>
        /// Retrieves names of all predefined exercises.
        /// </summary>
        /// <returns>A list of exercise names.</returns>
        public List<string> GetPredefinedExercises()
        {
            return this.storage.GetAllExerciseNames();
        }

        /// <summary>
        /// Assigns a nutrition plan to a client.
        /// </summary>
        /// <param name="plan">The nutrition plan to assign.</param>
        /// <param name="clientId">The client's ID.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
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