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

        
        public void assignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }
    }
}
