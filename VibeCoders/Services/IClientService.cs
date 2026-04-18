using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VibeCoders.Models;
using VibeCoders.Models.Integration;

namespace VibeCoders.Services
{
    public interface IClientService
    {
        bool FinalizeWorkout(WorkoutLog log);

        bool SaveSet(WorkoutLog log, string exerciseName, LoggedSet set);

        bool ModifyWorkout(WorkoutLog updatedLog);

        Task<bool> SyncNutritionAsync(
            NutritionSyncPayload payload,
            CancellationToken cancellationToken = default);

        NutritionPlan? GetActiveNutritionPlan(int clientId);

        List<Notification> GetNotifications(int clientId);

        void ConfirmDeload(Notification notification);
    }
}