namespace VibeCoders.Models.Integration;

public class NutritionSyncOptions
{
    public string Endpoint { get; set; } = "http://localhost:5000/api/nutrition/sync";
}
