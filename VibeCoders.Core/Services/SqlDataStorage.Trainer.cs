using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Services;
using User = VibeCoders.Models.User;
using Microsoft.Data.SqlClient;

namespace VibeCoders.Services
{

    public partial class SqlDataStorage : IDataStorage
    {


       
        public List<Client> GetTrainerClient(int trainerId)
        {
            var roster = new List<Client>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        c.client_id, 
                        u.username, 
                        c.weight, 
                        c.height,
                        (SELECT MAX(date) FROM WORKOUT_LOG wl WHERE wl.client_id = c.client_id) AS LastWorkoutDate
                    FROM CLIENT c
                    JOIN [USER] u ON c.user_id = u.id
                    WHERE c.trainer_id = @TrainerId;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TrainerId", trainerId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var client = new Client
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Weight = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                                Height = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                                WorkoutLog = new List<WorkoutLog>()
                            };

                            // Attaching the last workout date to satisfy the UI requirement
                            if (!reader.IsDBNull(4))
                            {
                                client.WorkoutLog.Add(new WorkoutLog { Date = reader.GetDateTime(4) });

                            }

                            roster.Add(client);
                        }
                    }
                }
            }
            return roster;
        }


        public bool SaveUser(User u)
        {
            //TODO
            return false;
        }

        public User? LoadUser(string username)
        {
            //TODO
            return null;
        }

        public bool SaveClientData(Client c)
        {
            //TODO
            return false;
        }


        //public bool saveNutritionPlan(NutritionPlan plan)
        //{
        //    throw new NotImplementedException("Nutrition team is working on this!");
        //}




    }
}