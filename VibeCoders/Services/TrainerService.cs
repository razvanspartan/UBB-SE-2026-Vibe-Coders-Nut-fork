using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Services;

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
}