namespace VibeCoders.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VibeCoders.Models;
    using VibeCoders.Repositories;
    using VibeCoders.Repositories.Interfaces;
    using VibeCoders.Services.Interfaces;

    public sealed class TrainerService : ITrainerService
    {
        private readonly IRepositoryWorkoutTemplate workoutTemplateRepository;
        private readonly IRepositoryWorkoutLog workoutLogRepository;
        private readonly IRepositoryTrainer trainerRepository;
        private readonly IRepositoryNutrition nutritionRepository;
        public TrainerService(IRepositoryWorkoutTemplate workoutTemplateRepository, IRepositoryWorkoutLog workoutLogRepository, IRepositoryTrainer trainerRepository, IRepositoryNutrition nutritionRepository)
        {
            this.workoutTemplateRepository = workoutTemplateRepository;
            this.workoutLogRepository = workoutLogRepository;
            this.trainerRepository = trainerRepository;
            this.nutritionRepository = nutritionRepository;
        }

        public List<Client> GetAssignedClients(int trainerId)
        {
            return trainerRepository.GetTrainerClients(trainerId);
        }

        public List<WorkoutLog> GetClientWorkoutHistory(int clientId)
        {
            return workoutLogRepository.GetWorkoutHistory(clientId);
        }

        public bool SaveWorkoutFeedback(WorkoutLog log)
        {
            if (log is null)
            {
                return false;
            }

            return workoutLogRepository.UpdateWorkoutLogFeedback(log.Id, log.Rating, log.TrainerNotes);
        }

        public void AssignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }

        public List<WorkoutTemplate> GetAvailableWorkouts(int clientId)
        {
            return workoutTemplateRepository.GetAvailableWorkouts(clientId);
        }

        public bool DeleteWorkoutTemplate(int templateId)
        {
            return trainerRepository.DeleteWorkoutTemplate(templateId);
        }

        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            if (template is null)
            {
                return false;
            }

            return trainerRepository.SaveTrainerWorkout(template);
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

            bool isSaved = trainerRepository.SaveTrainerWorkout(newTemplate);
            if (!isSaved)
            {
                return (false, "Could not save routine to database.");
            }

            return (true, string.Empty);
        }

        public List<string> GetAllExerciseNames()
        {
            return workoutTemplateRepository.GetAllExerciseNames();
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

            nutritionRepository.SaveNutritionPlanForClient(plan, clientId);
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