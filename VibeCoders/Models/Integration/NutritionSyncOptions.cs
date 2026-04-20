namespace VibeCoders.Models.Integration;

public class NutritionSyncOptions
{
    private const string DefaultEndpoint = "http://localhost:5000/api/nutrition/sync";

    public string Endpoint { get; set; } = DefaultEndpoint;
}