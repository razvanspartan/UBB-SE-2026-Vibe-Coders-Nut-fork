using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        /// <summary>
        /// Creates SQL Server tables used by the app when missing (idempotent).
        /// </summary>
        void EnsureSchemaCreated();

        // ── User ────────────────────────────────────────────────────────────
        bool SaveUser(User u);
        User? LoadUser(string username);

        // ── Client ──────────────────────────────────────────────────────────
        bool SaveClientData(Client c);
        List<Client> GetTrainerClient(int trainerId);

        // ── Workout Template ─────────────────────────────────────────────────
        /// <summary>
        /// Returns all workout templates available to the client:
        /// PREBUILT templates visible to everyone, plus any TRAINER_ASSIGNED
        /// templates assigned specifically to this client.
        /// </summary>
        List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

        // ── Workout Log ─────────────────────────────────────────────────────
        bool SaveWorkoutLog(WorkoutLog log);
        List<WorkoutLog> GetWorkoutHistory(int clientId);
        List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId);

        // ── Template Exercise ────────────────────────────────────────────────
        TemplateExercise? GetTemplateExercise(int templateExerciseId);
        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        // ── Notifications ────────────────────────────────────────────────────
        bool SaveNotification(Notification notification);
        List<Notification> GetNotifications(int clientId);

        // ── Achievements ─────────────────────────────────────────────────────
        /// <summary>
        /// Returns the full achievement catalog for <paramref name="clientId"/>, each row
        /// including whether the client has unlocked it. Locked rows are included so the
        /// rank showcase can keep future goals visible (product requirement).
        /// </summary>
        /// <param name="clientId">Client whose <c>CLIENT_ACHIEVEMENT</c> rows join the catalog.</param>
        List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId);

        
        bool AwardAchievement(int clientId, int achievementId);
    }
}