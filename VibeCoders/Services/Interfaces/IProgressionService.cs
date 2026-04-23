namespace VibeCoders.Services.Interfaces;

using VibeCoders.Models;

public interface IProgressionService
{
    void EvaluateWorkout(WorkoutLog log);

    void ProcessDeloadConfirmation(Notification notification);
}