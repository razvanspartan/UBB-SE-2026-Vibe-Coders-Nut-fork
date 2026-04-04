using VibeCoders.Models;

namespace VibeCoders.Services
{
    public interface IClientDataRepository
    {
        List<LoggedExercise> GetLoggedExercisesForClient(int clientId);
        List<Meal> GetMealsForClient(int clientId);
    }
}