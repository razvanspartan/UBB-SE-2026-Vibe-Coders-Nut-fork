namespace VibeCoders.Services.Interfaces;

using VibeCoders.Models;

public interface ITrainerService
{
    List<Client> GetAssignedClients(int trainerId);

    List<WorkoutLog> GetClientWorkoutHistory(int clientId);

    bool SaveWorkoutFeedback(WorkoutLog log);

    void AssignWorkout(Client client, WorkoutLog workout);

    List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

    bool DeleteWorkoutTemplate(int templateId);

    bool SaveTrainerWorkout(WorkoutTemplate template);

    (bool Success, string ErrorMessage) AssignNewRoutine(int? editingTemplateId, int clientId, string routineName, IEnumerable<TemplateExercise> exercises);

    List<string> GetAllExerciseNames();

    bool AssignNutritionPlan(NutritionPlan plan, int clientId);

    bool CreateAndAssignNutritionPlan(DateTime startDate, DateTime endDate, int clientId);
}