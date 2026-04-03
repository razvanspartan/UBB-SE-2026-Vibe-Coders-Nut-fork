using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public class TrainerService
    {
       
        public IDataStorage DataStorage { get; }

     
        public TrainerService(IDataStorage storage)
        {
            DataStorage = storage;
        }

        
        public List<Client> GetAssignedClients(int trainerId)
        {
            
            return DataStorage.GetTrainerClient(trainerId);
        }

        public List<WorkoutLog> GetClientWorkoutHistory(int clientId)
        {
            
            return DataStorage.GetWorkoutHistory(clientId);
        }

        public bool SaveWorkoutFeedback(WorkoutLog log)
        {
            if (log == null) return false;

            return DataStorage.UpdateWorkoutLogFeedback(log.Id, log.Rating, log.TrainerNotes);
        }

        public void assignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }

        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            if (template == null) return false;

            return DataStorage.SaveTrainerWorkout(template);
        }

        public List<string> GetPredefinedExercises()
        {
            return DataStorage.GetAllExerciseNames();
        }

    }
}
