using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using User = VibeCoders.Models.User;


namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        bool SaveUser(User u);
        User LoadUser(string username);
        bool SaveClientData(Client c);
        bool SaveWorkoutLog(WorkoutLog log);
        List<Client> GetTrainerClient(int trainerId);
    }
}
