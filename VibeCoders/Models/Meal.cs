namespace VibeCoders.Models;

public class Meal
{
    public int MealId { get; set; }
    public int NutritionPlanId { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<string> Ingredients { get; set; } = new ();

    public string Instructions { get; set; } = string.Empty;
}
