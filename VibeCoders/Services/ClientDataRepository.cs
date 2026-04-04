using VibeCoders.Models;
using Microsoft.Data.Sqlite;

namespace VibeCoders.Services
{
    public class ClientDataRepository : IClientDataRepository
    {
        private readonly string _connectionString;

        public ClientDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<LoggedExercise> GetLoggedExercisesForClient(int clientId)
        {
            var exercises = new List<LoggedExercise>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM LoggedExercise WHERE ClientId = $clientId";
            cmd.Parameters.AddWithValue("$clientId", clientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                exercises.Add(new LoggedExercise
                {
                    Id = reader.GetInt32(0),
                    ExerciseName = reader.GetString(1),
                    // Map other fields...
                });
            }

            // TODO: Load LoggedSets for each exercise from SQLite here

            return exercises;
        }

        public List<Meal> GetMealsForClient(int clientId)
        {
            var meals = new List<Meal>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM Meal WHERE NutritionPlanId = 
                                (SELECT NutritionPlanId FROM Client WHERE Id = $clientId)";
            cmd.Parameters.AddWithValue("$clientId", clientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                meals.Add(new Meal
                {
                    MealId = reader.GetInt32(0),
                    Name = reader.GetString(2),
                    // TODO: Load Ingredients and Instructions
                });
            }

            return meals;
        }
    }
}