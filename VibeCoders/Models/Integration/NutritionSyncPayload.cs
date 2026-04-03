using System.Text.Json.Serialization;

namespace VibeCoders.Models.Integration;

public sealed class NutritionSyncPayload
{
    [JsonPropertyName("totalCalories")]
    public int TotalCalories { get; init; }

    [JsonPropertyName("workoutDifficulty")]
    public string WorkoutDifficulty { get; init; } = string.Empty;

    [JsonPropertyName("userBmi")]
    public float UserBmi { get; init; }
}
