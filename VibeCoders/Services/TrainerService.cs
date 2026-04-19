namespace VibeCoders.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VibeCoders.Models;

    public sealed class TrainerService
    {
        private readonly IDataStorage storage;

        public TrainerService(IDataStorage storage)
        {
            this.storage = storage;
        }

        public List<Client> GetAssignedClients(int trainerId)
        {
            return storage.GetTrainerClient(trainerId);
        }

        public List<WorkoutLog> GetClientWorkoutHistory(int clientId)
        {
            return storage.GetWorkoutHistory(clientId);
        }

        public bool SaveWorkoutFeedback(WorkoutLog log)
        {
            if (log is null)
            {
                return false;
            }

            return storage.UpdateWorkoutLogFeedback(log.Id, log.Rating, log.TrainerNotes);
        }

        public void AssignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }

        public List<WorkoutTemplate> GetAvailableWorkouts(int clientId)
        {
            return storage.GetAvailableWorkouts(clientId);
        }

        public bool DeleteWorkoutTemplate(int templateId)
        {
            return storage.DeleteWorkoutTemplate(templateId);
        }

        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            if (template is null)
            {
                return false;
            }

            return storage.SaveTrainerWorkout(template);
        }

        public (bool Success, string ErrorMessage) AssignNewRoutine(int? editingTemplateId, int clientId, string routineName, IEnumerable<TemplateExercise> exercises)
        {
            if (string.IsNullOrWhiteSpace(routineName))
            {
                return (false, "Routine Name cannot be empty.");
            }

            if (exercises == null || !exercises.Any())
            {
                return (false, "You must add at least one exercise to the routine.");
            }

            var newTemplate = new WorkoutTemplate
            {
                Id = editingTemplateId ?? 0,
                ClientId = clientId,
                Name = routineName,
                Type = WorkoutType.TRAINER_ASSIGNED,
            };

            foreach (var exercise in exercises)
            {
                newTemplate.AddExercise(exercise);
            }

            bool isSaved = storage.SaveTrainerWorkout(newTemplate);
            if (!isSaved)
            {
                return (false, "Could not save routine to database.");
            }

            return (true, string.Empty);
        }

        public List<string> GetAllExerciseNames()
        {
            return storage.GetAllExerciseNames();
        }

        public bool AssignNutritionPlan(NutritionPlan plan, int clientId)
        {
            if (plan is null)
            {
                return false;
            }

            if (clientId <= 0)
            {
                return false;
            }

            storage.SaveNutritionPlanForClient(plan, clientId);
            return true;
        }

        public bool CreateAndAssignNutritionPlan(DateTime startDate, DateTime endDate, int clientId)
        {
            if (clientId <= 0)
            {
                return false;
            }

            var plan = new NutritionPlan
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date,
            };

            return AssignNutritionPlan(plan, clientId);
        }
    }
}