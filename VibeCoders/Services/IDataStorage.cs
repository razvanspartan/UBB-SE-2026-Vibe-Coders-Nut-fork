using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        void SaveWorkoutLog(WorkoutLog log);
        void SaveLoggedSet(LoggedSet set);
        void UpdateTemplateWeight(int templateExId, double newWeight);
        void SaveNotification(Models.Notification n);
        List<WorkoutLog> GetLastTwoLogsForExercise(string exName);

        TemplateExercise GetTemplateExercise(int exerciseId);
    }
}